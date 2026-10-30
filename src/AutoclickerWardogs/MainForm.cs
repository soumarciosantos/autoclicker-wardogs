using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace AutoclickerWardogs
{
    class MainForm : Form
    {
        readonly Config cfg = Config.Carregar();
        readonly Clicker clicker = new Clicker();
        readonly Stopwatch sessao = new Stopwatch();
        readonly Stopwatch relogioStatus = Stopwatch.StartNew();
        readonly Dictionary<string, Pagina> paginas = new Dictionary<string, Pagina>();
        readonly Dictionary<string, BotaoMilitar> abas = new Dictionary<string, BotaoMilitar>();
        readonly Dictionary<string, string> titulos = new Dictionary<string, string>
        {
            ["Painel"] = "Painel de comando", ["Ajustes"] = "Ajustes de operação", ["Atualizações"] = "Atualizações", ["Sobre"] = "Sobre Wardogs"
        };
        readonly Timer tTeclas = new Timer { Interval = 40 };
        readonly Timer tStatus = new Timer { Interval = 250 };
        readonly Timer tSegundo = new Timer { Interval = 1000 };
        readonly Timer tContagem = new Timer { Interval = 1000 };
        readonly Timer tVerificar = new Timer { Interval = 30 * 60 * 1000 }; // confere atualizações a cada 30 min
        readonly bool atualizado;

        const int LarguraLateral = 196;
        // Marca original do jogo, retirada do press kit oficial da Team17 (setembro de 2026).
        static readonly Image LogoWardogs = CarregarLogoWardogs();
        const string LinkDoacao = "https://livepix.gg/marciosantos"; // perfil confirmado pelo usuário
        const string LinkDiscord = "https://discord.gg/SUnKAKVKD5"; // convite fornecido pelo usuário

        static Image CarregarLogoWardogs()
        {
            using (var recurso = typeof(MainForm).Assembly.GetManifestResourceStream("WD_Fullmark_Full.png"))
            {
                if (recurso == null) throw new FileNotFoundException("Marca WARDOGS não incorporada ao aplicativo.");
                using (var original = Image.FromStream(recurso)) return new Bitmap(original);
            }
        }

        static PictureBox Marca(int x, int y, int largura, int altura) => new PictureBox
        {
            Bounds = new Rectangle(x, y, largura, altura), Image = LogoWardogs,
            SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Transparent,
            AccessibleName = "Marca oficial do jogo WARDOGS"
        };

        Point alvo;
        Color[] referencia;
        int trocada, contagem;
        double frac, cps;
        long ultimoTotal;
        bool prevF8, prevF9, novaSessao = true;
        InfoAtualizacao remoto;
        // aviso de nova versão: fica pendente enquanto estiver clicando e aparece uma vez por versão
        bool avisoPendente, avisoAberto, instalando;
        string avisoMostradoPara;

        PulsoVisual pulso;
        Estencil lblTitulo, lblStatus, lblTotal;
        Label lblDica, cardAgora, cardMedia, cardTempo, cardTela, lblAlvo, lblSalvo, lblAtual;
        Grafico grafico;
        BotaoMilitar btnIniciar, btnParar, btnVerificar, btnInstalar, btnNovaVersao;
        ListBox log;
        RichTextBox rtb;
        NumericUpDown numIntervalo, numTremor, numSaidaMs, numSaidaPx, numLimiar, numChecagens;
        Alternador chkAuto, chkTopo, chkBipe, chkVerificar;

        public MainForm(bool atualizado)
        {
            this.atualizado = atualizado;
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = $"Autoclicker Wardogs v{Updater.VersaoTexto}";
            ClientSize = new Size(980, 700);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Tema.Fundo;
            ForeColor = Tema.Texto;
            DoubleBuffered = true;
            TopMost = cfg.SempreNoTopo;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            MontarLateral();
            MontarTopo();

            foreach (var nome in new[] { "Painel", "Ajustes", "Atualizações", "Sobre" })
            {
                var p = new Pagina
                {
                    Bounds = new Rectangle(LarguraLateral + 20, 82, 748, 600),
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                    AutoScroll = true, AutoScrollMinSize = new Size(732, 588), Visible = false
                };
                Controls.Add(p);
                paginas[nome] = p;
            }
            MontarPainel(paginas["Painel"]);
            MontarAjustes(paginas["Ajustes"]);
            MontarAtualizacoes(paginas["Atualizações"]);
            MontarSobre(paginas["Sobre"]);

            tTeclas.Tick += (s, e) => LerTeclas();
            tStatus.Tick += (s, e) => AtualizarNumeros();
            tSegundo.Tick += (s, e) => CadaSegundo();
            tContagem.Tick += (s, e) => PassoContagem();
            tVerificar.Tick += (s, e) => { if (cfg.VerificarAoAbrir && !instalando) VerificarAtualizacao(true); };
            ResumeLayout(false);
        }

        // ---------- moldura própria ----------
        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_MINIMIZEBOX = 0x20000, WS_SYSMENU = 0x80000, CS_DROPSHADOW = 0x20000;
                var cp = base.CreateParams;
                cp.Style |= WS_MINIMIZEBOX | WS_SYSMENU;   // minimizar e restaurar pela barra de tarefas
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        // arrastar a janela pelo topo e pela barra lateral (fora dos botões)
        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84, HTCLIENT = 1, HTCAPTION = 2;
            base.WndProc(ref m);
            if (m.Msg == WM_NCHITTEST && (int)m.Result == HTCLIENT)
            {
                long lp = m.LParam.ToInt64();
                var p = PointToClient(new Point((short)(lp & 0xFFFF), (short)((lp >> 16) & 0xFFFF)));
                float s = DeviceDpi / 96f;
                if (p.Y < 52 * s || p.X < LarguraLateral * s) m.Result = (IntPtr)HTCAPTION;
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            float s = DeviceDpi / 96f;
            g.Clear(Tema.Fundo);
            var lateral = new RectangleF(0, 0, LarguraLateral * s, Height);
            using (var b = new SolidBrush(Tema.Lateral))
                g.FillRectangle(b, lateral);
            using (var p = new Pen(Tema.Borda, 1f * s))
                g.DrawLine(p, lateral.Right, 0, lateral.Right, Height);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            using (var p = new Pen(Tema.Borda)) g.DrawLine(p, lateral.Right + 22 * s, 66 * s, Width - 20 * s, 66 * s);
            using (var faixa = new SolidBrush(Tema.Destaque)) g.FillRectangle(faixa, lateral.Right + 20 * s, 65 * s, 110 * s, 2 * s);
        }

        void MontarLateral()
        {
            Controls.AddRange(new Control[] { Marca(16, 23, 164, 42),
                Tema.Rotulo("AUTO CLICK", 20, 76, 160, 20, 8, Tema.Destaque, true),
                Tema.Rotulo("CENTRAL WARDOGS", 20, 139, 160, 22, 8, Tema.Sub, true) });

            string[] nomes = { "Painel", "Ajustes", "Atualizações", "Sobre" };
            for (int i = 0; i < nomes.Length; i++)
            {
                string nome = nomes[i];
                var b = new BotaoMilitar { Text = nome == "Painel" ? "Painel" : nome == "Ajustes" ? "Ajustes" : nome,
                    Bounds = new Rectangle(14, 172 + i * 50, 168, 42), Estilo = EstiloBotao.Aba };
                b.Click += (s, e) => MostrarPagina(nome);
                Controls.Add(b);
                abas[nome] = b;
            }
            Controls.Add(Tema.Rotulo("ATALHOS", 20, 552, 150, 18, 8, Tema.Sub, true));
            Controls.Add(Tema.Rotulo("F8  Iniciar / pausar", 20, 578, 160, 22, 9, Tema.Texto));
            Controls.Add(Tema.Rotulo("F9  Parar", 20, 602, 160, 22, 9, Tema.Texto));
            Controls.Add(new Label
            {
                Text = $"WARDOGS   /   v{Updater.VersaoTexto}", Bounds = new Rectangle(20, 658, 160, 20),
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom, Font = Tema.Fonte(9),
                ForeColor = Tema.Sub, BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleLeft
            });
        }

        void MontarTopo()
        {
            lblTitulo = new Estencil { Bounds = new Rectangle(LarguraLateral + 20, 16, 350, 42), Font = Tema.Fonte(18, true), ForeColor = Tema.Texto };
            // aparece quando existe versão nova e a pessoa escolheu "Depois"
            btnNovaVersao = new BotaoMilitar
            {
                Bounds = new Rectangle(644, 16, 210, 34), Anchor = AnchorStyles.Top | AnchorStyles.Right, Estilo = EstiloBotao.Janela,
                Cor = CorAtualizar, Font = Tema.Fonte(11, true), Visible = false
            };
            btnNovaVersao.Click += (s, e) => { MostrarPagina("Atualizações"); InstalarAtualizacao(); };
            Controls.Add(btnNovaVersao);
            var minimizar = new BotaoMilitar { Text = "–", Bounds = new Rectangle(886, 17, 34, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right, Estilo = EstiloBotao.Janela, Cor = Tema.Lateral, AccessibleName = "Minimizar" };
            var fechar = new BotaoMilitar { Text = "×", Bounds = new Rectangle(930, 17, 34, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right, Estilo = EstiloBotao.Janela, Cor = Tema.Lateral, AccessibleName = "Fechar" };
            minimizar.Click += (s, e) => WindowState = FormWindowState.Minimized;
            fechar.Click += (s, e) => Close();
            Controls.AddRange(new Control[] { lblTitulo, minimizar, fechar });
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            var area = Screen.FromControl(this).WorkingArea;
            if (Width > area.Width - 16) Width = Math.Max(620, area.Width - 16);
            if (Height > area.Height - 16)
            {
                Height = Math.Max(480, area.Height - 16);
            }
            Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
            MostrarPagina("Painel");
            tTeclas.Start(); tStatus.Start(); tSegundo.Start(); tVerificar.Start();
            Log($"Autoclicker v{Updater.VersaoTexto} aberto");
            if (atualizado) Log($"Atualizado para v{Updater.VersaoTexto}");
            if (cfg.VerificarAoAbrir) VerificarAtualizacao(true);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            clicker.Parar();
            tTeclas.Stop(); tStatus.Stop(); tSegundo.Stop(); tContagem.Stop(); tVerificar.Stop();
            base.OnFormClosing(e);
        }

        void MostrarPagina(string nome)
        {
            foreach (var k in paginas.Keys)
            {
                paginas[k].Visible = k == nome;
                abas[k].Ativo = k == nome;
            }
            lblTitulo.Text = titulos[nome];
        }

        // ---------- Painel ----------
        void MontarPainel(Panel pp)
        {
            var estado = new PlacaMetal { Bounds = new Rectangle(0, 0, 732, 220), Hero = true, Acento = Tema.Destaque };
            estado.Controls.Add(Tema.Rotulo("WARDOGS   /   CONTROLE DE SESSÃO", 26, 18, 450, 22, 9, Tema.Destaque, true));
            lblStatus = new Estencil { Text = "PRONTO PARA INICIAR", Bounds = new Rectangle(26, 48, 465, 45), Font = Tema.Fonte(21, true), ForeColor = Tema.Texto };
            lblDica = Tema.Rotulo("Coloque o mouse no card do Wardogs e aperte F8.", 26, 98, 450, 42, 10.5f, Tema.Sub);
            pulso = new PulsoVisual { Bounds = new Rectangle(536, 20, 166, 156) };
            lblTotal = new Estencil { Text = "0", Bounds = new Rectangle(22, 44, 122, 62), Font = Tema.Fonte(34, true), ForeColor = Tema.Destaque, Alinhamento = StringAlignment.Center };
            var totalTitulo = Tema.Rotulo("CLIQUES", 33, 102, 100, 20, 8, Tema.Sub, true);
            totalTitulo.TextAlign = ContentAlignment.MiddleCenter;
            pulso.Controls.AddRange(new Control[] { lblTotal, totalTitulo });
            btnIniciar = Tema.Botao("Iniciar  ·  F8", 26, 158, 200, 44, Tema.Destaque);
            btnParar = Tema.Botao("Parar  ·  F9", 236, 158, 150, 44, Tema.Lateral);
            estado.Controls.AddRange(new Control[] { lblStatus, lblDica, pulso, btnIniciar, btnParar });
            pp.Controls.Add(estado);

            var metricas = new PlacaMetal { Bounds = new Rectangle(0, 232, 732, 82) };
            cardAgora = Metrica(metricas, "AGORA / S", 18);
            cardMedia = Metrica(metricas, "MÉDIA / S", 198);
            cardTempo = Metrica(metricas, "TEMPO", 378);
            cardTela = Metrica(metricas, "TELA MUDOU", 558);
            foreach (int x in new[] { 185, 365, 545 })
                metricas.Controls.Add(new Label { Bounds = new Rectangle(x, 20, 1, 42), BackColor = Tema.Borda });
            pp.Controls.Add(metricas);

            lblAlvo = Tema.Rotulo("Alvo: nenhum", 4, 324, 724, 28, 9.5f, Tema.Sub);
            grafico = new Grafico { Bounds = new Rectangle(0, 358, 732, 116) };

            var placaLog = new PlacaMetal { Bounds = new Rectangle(0, 486, 732, 100) };
            placaLog.Controls.Add(Tema.Rotulo("HISTÓRICO DA SESSÃO", 16, 8, 500, 18, 8.5f, Tema.Sub, true));
            log = new ListBox
            {
                Bounds = new Rectangle(16, 32, 700, 55), BackColor = Tema.Painel, ForeColor = Tema.Texto,
                BorderStyle = BorderStyle.None, Font = Tema.FonteMono(9)
            };
            placaLog.Controls.Add(log);
            pp.Controls.AddRange(new Control[] { lblAlvo, grafico, placaLog });

            // botão Iniciar: contagem de 3 s para dar tempo de levar o mouse até o card
            btnIniciar.Click += (s, e) =>
            {
                if (clicker.Rodando) { Pausar(); return; }
                contagem = 3;
                Status("INICIANDO EM 3...", Tema.Amarelo, Humor.Parado, "Leve o mouse até o card do time.");
                tContagem.Start();
            };
            btnParar.Click += (s, e) => Parar();
        }

        static Label Metrica(Control pai, string titulo, int x)
        {
            pai.Controls.Add(Tema.Rotulo(titulo, x, 12, 162, 18, 8.5f, Tema.Sub, true));
            var valor = Tema.Rotulo("0", x, 32, 162, 36, 19, Tema.Texto, true);
            pai.Controls.Add(valor);
            return valor;
        }

        void Log(string msg)
        {
            log.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}");
            while (log.Items.Count > 200) log.Items.RemoveAt(200);
        }

        void Status(string texto, Color cor, Humor humor, string dica = null)
        {
            lblStatus.Text = texto;
            lblStatus.ForeColor = cor;
            pulso.Humor = humor;
            if (dica != null) lblDica.Text = dica;
        }

        void PassoContagem()
        {
            contagem--;
            if (contagem <= 0) { tContagem.Stop(); Iniciar(); }
            else Status($"INICIANDO EM {contagem}...", Tema.Amarelo, Humor.Parado, "Leve o mouse até o card do time.");
        }

        void CancelarContagem()
        {
            if (contagem > 0) { tContagem.Stop(); contagem = 0; }
        }

        void Iniciar()
        {
            Native.GetCursorPos(out var p);
            if (Bounds.Contains(p.X, p.Y))
            {
                Status("PRONTO PARA INICIAR", Tema.Texto, Humor.Parado, "O mouse está em cima desta janela. Leve até o card do time e aperte F8.");
                return;
            }
            if (novaSessao)
            {
                clicker.Zerar(); sessao.Reset(); grafico.Dados.Clear();
                ultimoTotal = 0; novaSessao = false;
            }
            alvo = new Point(p.X, p.Y);
            try { referencia = ScreenWatcher.Amostra(alvo); } catch { referencia = null; }
            trocada = 0; frac = 0;
            AplicarAjustesDoClique();
            clicker.Iniciar(alvo.X, alvo.Y);
            sessao.Start();
            lblAlvo.Text = $"Alvo: ({alvo.X}, {alvo.Y})  |  {cfg.IntervaloMs} ms  |  ±{cfg.Tremor} px" +
                (cfg.SaidaMs > 0 ? $"  |  sai a cada {cfg.SaidaMs / 1000.0:0.#} s" : "");
            btnIniciar.Text = "Pausar (F8)";
            btnIniciar.Cor = Tema.Amarelo;
            Status("EM ATIVIDADE", Tema.Verde, Humor.Clicando, "F8 pausa  |  F9 para");
            Log($"Clicando em ({alvo.X}, {alvo.Y})");
            if (cfg.IntervaloMs < 10)
                Log($"Aviso: intervalo de {cfg.IntervaloMs} ms é rápido demais para o jogo; use 15 ms em Ajustes");
        }

        void AplicarAjustesDoClique()
        {
            clicker.DelayMs = cfg.IntervaloMs;
            clicker.Tremor = cfg.Tremor;
            clicker.SaidaMs = cfg.SaidaMs;
            clicker.SaidaPx = cfg.SaidaPx;
        }

        void Pausar()
        {
            clicker.Parar(); sessao.Stop();
            btnIniciar.Text = "Continuar (F8)";
            btnIniciar.Cor = Tema.Destaque;
            Status("PAUSADO", Tema.Amarelo, Humor.Pausado, "F8 continua no ponto atual do mouse  |  F9 encerra a sessão");
            Log($"Pausado - {clicker.Total:N0} cliques");
        }

        void Parar(bool entrou = false)
        {
            CancelarContagem();
            bool tinhaSessao = clicker.Rodando || sessao.ElapsedTicks > 0;
            clicker.Parar(); sessao.Stop(); novaSessao = true;
            btnIniciar.Text = "Iniciar (F8)";
            btnIniciar.Cor = Tema.Destaque;
            if (entrou)
            {
                Status("SESSÃO CONCLUÍDA", Tema.Verde, Humor.Feliz, "A tela de facção sumiu. Aperte F8 para uma nova sessão.");
                if (cfg.Bipe) Task.Run(() => { Console.Beep(1000, 250); Console.Beep(1400, 250); });
                Activate();
            }
            else
            {
                Status("PRONTO PARA INICIAR", Tema.Texto, Humor.Parado, "Coloque o mouse no card do Wardogs e aperte F8.");
            }
            if (tinhaSessao) Log($"Sessão encerrada - {clicker.Total:N0} cliques em {sessao.Elapsed:hh\\:mm\\:ss}");
        }

        void AlternarCliques()
        {
            CancelarContagem();
            if (clicker.Rodando) Pausar(); else Iniciar();
        }

        // F8/F9 funcionam mesmo com o jogo em foco
        void LerTeclas()
        {
            bool f8 = Native.TeclaApertada(Native.VK_F8);
            bool f9 = Native.TeclaApertada(Native.VK_F9);
            if (f8 && !prevF8) AlternarCliques();
            if (f9 && !prevF9) Parar();
            prevF8 = f8; prevF9 = f9;
        }

        void AtualizarNumeros()
        {
            long total = clicker.Total;
            double dt = Math.Max(relogioStatus.Elapsed.TotalSeconds, 0.001);
            cps = clicker.Rodando ? (total - ultimoTotal) / dt : 0;
            ultimoTotal = total;
            relogioStatus.Restart();
            double seg = Math.Max(sessao.Elapsed.TotalSeconds, 0.001);
            lblTotal.Text = total.ToString("N0");
            cardAgora.Text = cps.ToString("N0");
            cardMedia.Text = (total > 0 ? total / seg : 0).ToString("N0");
            cardTempo.Text = sessao.Elapsed.ToString(@"hh\:mm\:ss");
            cardTela.Text = frac.ToString("P0");
            // botão de nova versão piscando para chamar atenção
            if (btnNovaVersao.Visible && btnNovaVersao.Enabled)
                btnNovaVersao.Cor = (++piscar / 2) % 2 == 0 ? CorAtualizar : Color.FromArgb(214, 160, 40);
            MostrarAvisoSePuder();
        }

        static readonly Color CorAtualizar = Color.FromArgb(150, 112, 24);
        int piscar;

        // abre a janela de nova versão, mas só quando não estiver clicando (para não atrapalhar no jogo)
        void MostrarAvisoSePuder()
        {
            if (!avisoPendente || avisoAberto || instalando || remoto == null) return;
            if (clicker.Rodando || contagem > 0) return;
            avisoPendente = false;
            avisoMostradoPara = remoto.Versao;
            avisoAberto = true;
            try
            {
                using (var aviso = new AvisoAtualizacao(remoto))
                {
                    if (aviso.ShowDialog(this) == DialogResult.OK)
                    {
                        MostrarPagina("Atualizações");
                        InstalarAtualizacao();
                    }
                    else
                    {
                        Log("Atualização adiada - use o botão amarelo no topo quando quiser");
                    }
                }
            }
            finally
            {
                avisoAberto = false;
            }
        }

        // gráfico e detecção de "entrou no time", 1x por segundo
        void CadaSegundo()
        {
            if (!clicker.Rodando) return;
            grafico.Dados.Add(cps);
            while (grafico.Dados.Count > 60) grafico.Dados.RemoveAt(0);
            grafico.Invalidate();

            if (!cfg.AutoParar || referencia == null) return;
            try { frac = ScreenWatcher.FracaoMudada(referencia, ScreenWatcher.Amostra(alvo)); }
            catch { return; }
            trocada = frac >= cfg.Limiar / 100.0 ? trocada + 1 : 0;
            if (trocada >= cfg.Checagens) Parar(true);
        }

        // ---------- Ajustes ----------
        bool montandoAjustes;

        static PlacaMetal Secao(Control pai, string titulo, int x, int y, int altura)
        {
            var placa = new PlacaMetal { Bounds = new Rectangle(x, y, 358, altura), Acento = Tema.Oliva };
            placa.Controls.Add(Tema.Rotulo(titulo, 20, 14, 315, 28, 11, Tema.Destaque, true));
            pai.Controls.Add(placa);
            return placa;
        }

        // linha de ajuste: nome, dica curta embaixo e o número à direita
        NumericUpDown Linha(Control placa, string nome, string dica, int y, int min, int max, int valor, int passo = 1)
        {
            placa.Controls.Add(Tema.Rotulo(nome, 20, y, 230, 22, 10.5f));
            placa.Controls.Add(Tema.Rotulo(dica, 20, y + 24, 315, 32, 8.5f, Tema.Sub));
            var n = Tema.Numero(placa, 260, y + 2, min, max, valor);
            n.Increment = passo;
            n.ValueChanged += (s, e) => AoMudarAjuste();
            return n;
        }

        Alternador Chave(Control placa, string texto, int y, bool marcado)
        {
            var c = Tema.Caixa(placa, texto, 20, y, marcado);
            c.Width = 320;
            c.CheckedChanged += (s, e) => AoMudarAjuste();
            return c;
        }

        void MontarAjustes(Panel pa)
        {
            montandoAjustes = true;

            var cliques = Secao(pa, "01  /  Ritmo dos cliques", 0, 0, 190);
            numIntervalo = Linha(cliques, "Intervalo entre cliques (ms)", "Recomendado 15 ms. Abaixo de 10 o jogo perde cliques.", 54, 0, 1000, cfg.IntervaloMs);
            numTremor = Linha(cliques, "Movimento entre cliques (px)", "Anda para os lados a cada clique. 0 = parado.", 118, 0, 10, cfg.Tremor);

            var saida = Secao(pa, "02  /  Movimento do mouse", 374, 0, 190);
            numSaidaMs = Linha(saida, "Sair e voltar a cada (ms)", "Sai do card sem clicar e volta. 0 = desligado.", 54, 0, 10000, cfg.SaidaMs, 100);
            numSaidaPx = Linha(saida, "Distância do movimento (px)", "Alterna: uma vez desce, na outra sobe.", 118, 20, 600, cfg.SaidaPx, 10);

            var radar = Secao(pa, "03  /  Parada automática", 0, 206, 238);
            chkAuto = Chave(radar, "Parar sozinho quando entrar no time", 52, cfg.AutoParar);
            numLimiar = Linha(radar, "Mudança necessária na tela (%)", "Mais alto = mais difícil parar sem querer.", 98, 20, 95, cfg.Limiar);
            numChecagens = Linha(radar, "Conferências antes de parar", "Uma por segundo.", 162, 1, 30, cfg.Checagens);

            var janela = Secao(pa, "04  /  Janela e avisos", 374, 206, 174);
            chkTopo = Chave(janela, "Janela sempre por cima do jogo", 58, cfg.SempreNoTopo);
            chkBipe = Chave(janela, "Bipe ao entrar no time", 108, cfg.Bipe);

            lblSalvo = Tema.Rotulo("Alterações salvas automaticamente.", 378, 402, 350, 32, 9.5f, Tema.Verde);
            pa.Controls.Add(lblSalvo);
            montandoAjustes = false;
        }

        // cada mudança vale na hora e é salva, sem botão de salvar
        void AoMudarAjuste()
        {
            if (montandoAjustes) return;
            cfg.IntervaloMs = (int)numIntervalo.Value;
            cfg.Tremor = (int)numTremor.Value;
            cfg.SaidaMs = (int)numSaidaMs.Value;
            cfg.SaidaPx = (int)numSaidaPx.Value;
            cfg.AutoParar = chkAuto.Checked;
            cfg.Limiar = (int)numLimiar.Value;
            cfg.Checagens = (int)numChecagens.Value;
            cfg.SempreNoTopo = chkTopo.Checked;
            cfg.Bipe = chkBipe.Checked;
            AplicarAjustesDoClique();
            TopMost = cfg.SempreNoTopo;
            SalvarConfig();
        }

        void SalvarConfig()
        {
            try
            {
                cfg.Salvar();
                lblSalvo.ForeColor = Tema.Verde;
                lblSalvo.Text = $"✔ Salvo às {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                lblSalvo.ForeColor = Tema.Vermelho;
                lblSalvo.Text = "Erro ao salvar: " + ex.Message;
            }
        }

        // ---------- Atualizações ----------
        void MontarAtualizacoes(Panel pu)
        {
            var versao = new PlacaMetal { Bounds = new Rectangle(0, 0, 732, 148) };
            versao.Controls.Add(Tema.Rotulo("VERSÃO INSTALADA", 24, 18, 500, 22, 9, Tema.Destaque, true));
            versao.Controls.Add(Tema.Rotulo($"v{Updater.VersaoTexto}", 24, 42, 500, 46, 22, Tema.Texto, true));
            var link = new LinkLabel
            {
                Text = "Ver o projeto no GitHub ↗", Bounds = new Rectangle(24, 98, 360, 24), Font = Tema.Fonte(10),
                LinkColor = Tema.Destaque, ActiveLinkColor = Tema.Texto, BackColor = Tema.Painel
            };
            link.LinkClicked += (s, e) => Process.Start(Updater.Repositorio);
            versao.Controls.Add(link);
            pu.Controls.Add(versao);
            chkVerificar = Tema.Caixa(pu, "Avisar sobre versões novas (ao abrir e a cada 30 min)", 0, 160, cfg.VerificarAoAbrir);
            chkVerificar.Width = 700;
            btnVerificar = Tema.Botao("Verificar agora", 0, 204, 210, 42, Tema.Destaque);
            btnInstalar = Tema.Botao("Instalar atualização", 224, 204, 228, 42, Tema.Verde);
            btnInstalar.Enabled = false;
            lblAtual = Tema.Rotulo("Ainda não verificado.", 0, 250, 728, 28, 10, Tema.Sub);

            var placa = new PlacaMetal { Bounds = new Rectangle(0, 306, 732, 280) };
            rtb = new RichTextBox
            {
                Bounds = new Rectangle(18, 14, 696, 252), ReadOnly = true, BorderStyle = BorderStyle.None,
                BackColor = Tema.Painel, ForeColor = Tema.Texto, Font = Tema.Fonte(10)
            };
            placa.Controls.Add(rtb);
            pu.Controls.AddRange(new Control[] { btnVerificar, btnInstalar, lblAtual, Tema.Rotulo("Novidades", 0, 280, 500, 24, 12, Tema.Texto, true), placa });

            btnVerificar.Click += (s, e) => VerificarAtualizacao(false);
            btnInstalar.Click += (s, e) => InstalarAtualizacao();
            chkVerificar.CheckedChanged += (s, e) =>
            {
                cfg.VerificarAoAbrir = chkVerificar.Checked;
                try { cfg.Salvar(); } catch { }
            };

            try { MostrarNovidades(Updater.NovidadesLocais()); } catch { }
        }

        void MontarSobre(Panel ps)
        {
            var principal = new PlacaMetal { Bounds = new Rectangle(0, 0, 732, 220), Hero = true, Acento = Tema.Destaque };
            principal.Controls.Add(Marca(26, 20, 320, 64));
            principal.Controls.Add(Tema.Rotulo("AUTO CLICK  /  Controle. Ritmo. Precisão.", 26, 89, 650, 43, 18, Tema.Texto, true));
            principal.Controls.Add(Tema.Rotulo("Ferramenta independente para WARDOGS. Jogo desenvolvido pela BULKHEAD e publicado pela Team17.",
                26, 148, 650, 49, 10.5f, Tema.Sub));
            ps.Controls.Add(principal);

            var creditos = new PlacaMetal { Bounds = new Rectangle(0, 236, 732, 116) };
            creditos.Controls.Add(Tema.Rotulo("DESENVOLVIMENTO E AUTORIA", 26, 17, 640, 20, 9, Tema.Destaque, true));
            creditos.Controls.Add(Tema.Rotulo("HSP", 26, 40, 640, 54, 25, Tema.Texto, true));
            ps.Controls.Add(creditos);

            var apoio = new PlacaMetal { Bounds = new Rectangle(0, 368, 732, 188) };
            apoio.Controls.Add(Tema.Rotulo("Comunidade e apoio", 26, 20, 560, 34, 16, Tema.Texto, true));
            apoio.Controls.Add(Tema.Rotulo("Apoie o projeto ou visite a comunidade Wardogs em páginas externas.",
                26, 60, 660, 38, 10.5f, Tema.Sub));
            var doar = Tema.Botao("Doar ao HSP ↗", 26, 112, 220, 46, Tema.Destaque);
            doar.AccessibleName = "Abrir página de doação do HSP no navegador";
            doar.Click += (s, e) => Process.Start(new ProcessStartInfo(LinkDoacao) { UseShellExecute = true });
            apoio.Controls.Add(doar);
            var discord = Tema.Botao("Entrar no Discord ↗", 260, 112, 220, 46, Tema.Oliva);
            discord.AccessibleName = "Abrir convite do Discord no navegador";
            discord.Click += (s, e) => Process.Start(new ProcessStartInfo(LinkDiscord) { UseShellExecute = true });
            apoio.Controls.Add(discord);
            ps.Controls.Add(apoio);

            ps.Controls.Add(Tema.Rotulo($"Versão {Updater.VersaoTexto}  ·  Atalhos: F8 inicia/pausa, F9 para.", 4, 562, 700, 24, 9.5f, Tema.Sub));
        }

        void MostrarNovidades(Novidade[] lista)
        {
            rtb.Clear();
            if (lista == null) return;
            foreach (var n in lista)
            {
                bool nova = Version.TryParse(n.Versao, out var v) && v > Updater.Versao;
                rtb.SelectionIndent = 0;
                rtb.SelectionHangingIndent = 0;
                rtb.SelectionFont = Tema.Fonte(11, true);
                rtb.SelectionColor = nova ? Tema.Verde : Tema.Destaque;
                rtb.AppendText($"v{n.Versao}   {n.Data}\n");
                // recuo com deslocamento: as linhas quebradas começam alinhadas com o texto, depois do marcador
                rtb.SelectionIndent = 8;
                rtb.SelectionHangingIndent = 15;
                rtb.SelectionFont = Tema.Fonte(10.5f);
                rtb.SelectionColor = Tema.Texto;
                foreach (var item in n.Itens ?? new string[0]) rtb.AppendText($"▸  {item}\n");
                rtb.AppendText("\n");
            }
            rtb.SelectionStart = 0;
        }

        void Aviso(string texto, Color cor)
        {
            lblAtual.ForeColor = cor;
            lblAtual.Text = texto;
        }

        async void VerificarAtualizacao(bool aoAbrir)
        {
            btnVerificar.Enabled = false;
            Aviso("Verificando...", Tema.Sub);
            try
            {
                var info = await Task.Run(Updater.Verificar);
                remoto = info;
                if (info.Novidades != null) MostrarNovidades(info.Novidades);
                if (Updater.TemVersaoNova(info))
                {
                    Aviso($"Nova versão disponível: v{info.Versao}. Clique em Instalar atualização.", Tema.Verde);
                    btnInstalar.Enabled = true;
                    abas["Atualizações"].Text = "Atualizações  ●";
                    btnNovaVersao.Text = $"▲ Atualizar p/ v{info.Versao}";
                    btnNovaVersao.Visible = true;
                    if (avisoMostradoPara != info.Versao)
                    {
                        Log($"Atualização v{info.Versao} disponível");
                        avisoPendente = true;
                        MostrarAvisoSePuder();
                    }
                }
                else
                {
                    Aviso($"Você está na versão mais recente (v{Updater.VersaoTexto}). Verificado às {DateTime.Now:HH:mm}.", Tema.Sub);
                    btnInstalar.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                Aviso("Não foi possível verificar: " + ex.Message, Tema.Vermelho);
                if (aoAbrir) Log("Falha ao verificar atualizações");
            }
            finally
            {
                btnVerificar.Enabled = true;
            }
        }

        async void InstalarAtualizacao()
        {
            if (remoto == null || instalando) return;
            instalando = true;
            Parar();
            btnInstalar.Enabled = btnVerificar.Enabled = btnNovaVersao.Enabled = false;
            Aviso($"Baixando v{remoto.Versao}...", Tema.Sub);
            try
            {
                var info = remoto;
                await Task.Run(() => Updater.Instalar(info));
                Close();
            }
            catch (Exception ex)
            {
                Aviso("Atualização cancelada: " + ex.Message, Tema.Vermelho);
                btnInstalar.Enabled = btnVerificar.Enabled = btnNovaVersao.Enabled = true;
                instalando = false;
            }
        }
    }
}
