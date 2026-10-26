using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
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
            ["Painel"] = "// PAINEL DE COMBATE", ["Ajustes"] = "// AJUSTES DA MISSÃO", ["Atualizações"] = "// ATUALIZAÇÕES"
        };
        readonly Timer tTeclas = new Timer { Interval = 40 };
        readonly Timer tStatus = new Timer { Interval = 250 };
        readonly Timer tSegundo = new Timer { Interval = 1000 };
        readonly Timer tContagem = new Timer { Interval = 1000 };
        readonly bool atualizado;

        const int LarguraLateral = 196;
        static readonly Color FundoLista = Color.FromArgb(30, 34, 24);

        Point alvo;
        Color[] referencia;
        int trocada, contagem;
        double frac, cps;
        long ultimoTotal;
        bool prevF8, prevF9, novaSessao = true;
        InfoAtualizacao remoto;

        Mascote mascote;
        Estencil lblTitulo, lblStatus, lblTotal;
        Label lblDica, cardAgora, cardMedia, cardTempo, cardTela, lblAlvo, lblSalvo, lblAtual;
        Grafico grafico;
        BotaoMilitar btnIniciar, btnParar, btnVerificar, btnInstalar;
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
            ClientSize = new Size(690, 640);
            FormBorderStyle = FormBorderStyle.None;   // moldura própria, desenhada em OnPaint
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Tema.Fundo;
            ForeColor = Tema.Texto;
            DoubleBuffered = true;
            TopMost = cfg.SempreNoTopo;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            MontarLateral();
            MontarTopo();

            foreach (var nome in new[] { "Painel", "Ajustes", "Atualizações" })
            {
                var p = new Pagina { Bounds = new Rectangle(LarguraLateral + 16, 56, 462, 568), Visible = false };
                Controls.Add(p);
                paginas[nome] = p;
            }
            MontarPainel(paginas["Painel"]);
            MontarAjustes(paginas["Ajustes"]);
            MontarAtualizacoes(paginas["Atualizações"]);

            tTeclas.Tick += (s, e) => LerTeclas();
            tStatus.Tick += (s, e) => AtualizarNumeros();
            tSegundo.Tick += (s, e) => CadaSegundo();
            tContagem.Tick += (s, e) => PassoContagem();
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
            using (var camo = new TextureBrush(Tema.Camuflagem))
                g.FillRectangle(camo, ClientRectangle);

            // barra lateral: metal escuro sobre a camuflagem
            var lateral = new RectangleF(0, 0, LarguraLateral * s, Height);
            using (var b = new LinearGradientBrush(lateral, Color.FromArgb(236, 28, 31, 22), Color.FromArgb(244, 18, 20, 14), 0f))
                g.FillRectangle(b, lateral);
            using (var p = new Pen(Tema.Borda, 1.5f * s))
                g.DrawLine(p, lateral.Right, 0, lateral.Right, Height);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            Tecla(g, s, 430, "F8", "LIGA / PAUSA");
            Tecla(g, s, 466, "F9", "PARA");
            Tema.FaixaAlerta(g, new RectangleF(0, 560 * s, LarguraLateral * s, 12 * s));
            Tema.FaixaAlerta(g, new RectangleF(lateral.Right, 0, Width - lateral.Right, 4 * s));

            using (var p = new Pen(Tema.Borda))
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
        }

        void Tecla(Graphics g, float s, float y, string tecla, string acao)
        {
            var r = new RectangleF(22 * s, y * s, 38 * s, 26 * s);
            using (var forma = Tema.Chanfro(r, 5 * s))
            using (var b = new LinearGradientBrush(r, Color.FromArgb(70, 76, 54), Color.FromArgb(40, 44, 30), 90f))
            using (var borda = new Pen(Tema.Destaque, 1.2f * s))
            {
                g.FillPath(b, forma);
                g.DrawPath(borda, forma);
            }
            using (var fonte = Tema.Fonte(10, true))
            using (var fonteAcao = Tema.Fonte(10))
            using (var cor = new SolidBrush(Tema.Texto))
            using (var sub = new SolidBrush(Tema.Sub))
            using (var centro = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var esquerda = new StringFormat { LineAlignment = StringAlignment.Center })
            {
                g.DrawString(tecla, fonte, cor, r, centro);
                g.DrawString(acao, fonteAcao, sub, new RectangleF(r.Right + 10 * s, r.Y, 120 * s, r.Height), esquerda);
            }
        }

        void MontarLateral()
        {
            mascote = new Mascote { Bounds = new Rectangle(23, 14, 150, 150) };
            var marca = new Estencil
            {
                Text = "WARDOGS", Bounds = new Rectangle(0, 164, LarguraLateral, 42), Font = Tema.Fonte(26, true),
                ForeColor = Tema.Destaque, Alinhamento = StringAlignment.Center
            };
            var sub = new Label
            {
                Text = "AUTOCLICKER  DE  GUERRA", Bounds = new Rectangle(0, 204, LarguraLateral, 20), Font = Tema.Fonte(9),
                ForeColor = Tema.Sub, BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.AddRange(new Control[] { mascote, marca, sub });

            string[] nomes = { "Painel", "Ajustes", "Atualizações" };
            for (int i = 0; i < nomes.Length; i++)
            {
                string nome = nomes[i];
                var b = new BotaoMilitar { Text = nome, Bounds = new Rectangle(16, 242 + i * 50, 164, 42), Estilo = EstiloBotao.Aba };
                b.Click += (s, e) => MostrarPagina(nome);
                Controls.Add(b);
                abas[nome] = b;
            }

            Controls.Add(new Label
            {
                Text = $"v{Updater.VersaoTexto}", Bounds = new Rectangle(0, 590, LarguraLateral, 20), Font = Tema.Fonte(9),
                ForeColor = Tema.Sub, BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleCenter
            });
        }

        void MontarTopo()
        {
            lblTitulo = new Estencil { Bounds = new Rectangle(LarguraLateral + 16, 14, 380, 32), Font = Tema.Fonte(15, true), ForeColor = Tema.Texto };
            var minimizar = new BotaoMilitar { Text = "_", Bounds = new Rectangle(690 - 86, 14, 34, 28), Estilo = EstiloBotao.Janela, Cor = Tema.Oliva, AccessibleName = "Minimizar" };
            var fechar = new BotaoMilitar { Text = "X", Bounds = new Rectangle(690 - 46, 14, 34, 28), Estilo = EstiloBotao.Janela, Cor = Tema.Vermelho, AccessibleName = "Fechar" };
            minimizar.Click += (s, e) => WindowState = FormWindowState.Minimized;
            fechar.Click += (s, e) => Close();
            Controls.AddRange(new Control[] { lblTitulo, minimizar, fechar });
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            MostrarPagina("Painel");
            tTeclas.Start(); tStatus.Start(); tSegundo.Start();
            Log($"Autoclicker v{Updater.VersaoTexto} aberto");
            if (atualizado) Log($"Atualizado para v{Updater.VersaoTexto}");
            if (cfg.VerificarAoAbrir) VerificarAtualizacao(true);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            clicker.Parar();
            tTeclas.Stop(); tStatus.Stop(); tSegundo.Stop(); tContagem.Stop();
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
            lblStatus = new Estencil { Text = "AGUARDANDO ORDENS", Bounds = new Rectangle(0, 0, 456, 36), Font = Tema.Fonte(21, true), ForeColor = Tema.Sub };
            lblDica = Tema.Rotulo("Coloque o mouse no card do Wardogs e aperte F8.", 0, 36, 456, 20, 10, Tema.Sub);
            lblTotal = new Estencil { Text = "0", Bounds = new Rectangle(0, 56, 456, 60), Font = Tema.Fonte(42, true), ForeColor = Tema.Texto };
            pp.Controls.AddRange(new Control[] { lblStatus, lblDica, lblTotal, Tema.Rotulo("CLIQUES NESTA SESSÃO", 4, 116, 300, 22, 9, Tema.Sub) });

            cardAgora = Tema.Cartao(pp, "AGORA /S", 0, 144);
            cardMedia = Tema.Cartao(pp, "MÉDIA /S", 116, 144);
            cardTempo = Tema.Cartao(pp, "TEMPO", 232, 144);
            cardTela = Tema.Cartao(pp, "TELA MUDOU", 348, 144);

            lblAlvo = Tema.Rotulo("Alvo: nenhum", 0, 208, 456, 20, 9.5f, Tema.Sub);
            grafico = new Grafico { Bounds = new Rectangle(0, 232, 456, 120) };
            btnIniciar = Tema.Botao("Iniciar (F8)", 0, 362, 222, 44, Tema.Oliva);
            btnParar = Tema.Botao("Parar (F9)", 234, 362, 222, 44, Tema.Vermelho);

            var placaLog = new PlacaMetal { Bounds = new Rectangle(0, 416, 456, 148) };
            log = new ListBox
            {
                Bounds = new Rectangle(10, 10, 436, 128), BackColor = FundoLista, ForeColor = Tema.Texto,
                BorderStyle = BorderStyle.None, Font = Tema.FonteMono(9)
            };
            placaLog.Controls.Add(log);
            pp.Controls.AddRange(new Control[] { lblAlvo, grafico, btnIniciar, btnParar, placaLog });

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

        void Log(string msg)
        {
            log.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}");
            while (log.Items.Count > 200) log.Items.RemoveAt(200);
        }

        void Status(string texto, Color cor, Humor humor, string dica = null)
        {
            lblStatus.Text = texto;
            lblStatus.ForeColor = cor;
            mascote.Humor = humor;
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
                Status("AGUARDANDO ORDENS", Tema.Sub, Humor.Parado, "O mouse está em cima desta janela. Leve até o card do time e aperte F8.");
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
            btnIniciar.Cor = Color.FromArgb(150, 112, 24);
            Status("ATACANDO!", Tema.Destaque, Humor.Clicando, "F8 pausa  |  F9 para");
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
            btnIniciar.Cor = Tema.Oliva;
            Status("PAUSADO", Tema.Amarelo, Humor.Pausado, "F8 continua no ponto atual do mouse  |  F9 encerra a sessão");
            Log($"Pausado - {clicker.Total:N0} cliques");
        }

        void Parar(bool entrou = false)
        {
            CancelarContagem();
            bool tinhaSessao = clicker.Rodando || sessao.ElapsedTicks > 0;
            clicker.Parar(); sessao.Stop(); novaSessao = true;
            btnIniciar.Text = "Iniciar (F8)";
            btnIniciar.Cor = Tema.Oliva;
            if (entrou)
            {
                Status("MISSÃO CUMPRIDA!", Tema.Verde, Humor.Feliz, "A tela de facção sumiu. Aperte F8 para uma nova sessão.");
                if (cfg.Bipe) Task.Run(() => { Console.Beep(1000, 250); Console.Beep(1400, 250); });
                Activate();
            }
            else
            {
                Status("AGUARDANDO ORDENS", Tema.Sub, Humor.Parado, "Coloque o mouse no card do Wardogs e aperte F8.");
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
        void MontarAjustes(Panel pa)
        {
            pa.Controls.Add(Tema.Rotulo("Intervalo entre cliques (ms)", 0, 4, 300, 22, 11));
            numIntervalo = Tema.Numero(pa, 360, 2, 0, 1000, cfg.IntervaloMs);
            pa.Controls.Add(Tema.Rotulo("Recomendado: 15 ms (≈ 60/s). O jogo só lê um clique por quadro; abaixo de 10 ms ele começa a perder cliques e movimentos.", 0, 30, 456, 34, 9, Tema.Sub));
            pa.Controls.Add(Tema.Rotulo("Mexer o mouse entre cliques (px)", 0, 72, 300, 22, 11));
            numTremor = Tema.Numero(pa, 360, 70, 0, 10, cfg.Tremor);
            pa.Controls.Add(Tema.Rotulo("Anda esse tanto para a esquerda e para a direita a cada clique, para o jogo perceber o mouse no card. 0 = parado.", 0, 98, 456, 34, 9, Tema.Sub));
            pa.Controls.Add(Tema.Rotulo("Sair do card e voltar a cada (ms)", 0, 140, 300, 22, 11));
            numSaidaMs = Tema.Numero(pa, 360, 138, 0, 10000, cfg.SaidaMs);
            numSaidaMs.Increment = 100;
            pa.Controls.Add(Tema.Rotulo("Distância que o mouse anda (px)", 0, 176, 300, 22, 11));
            numSaidaPx = Tema.Numero(pa, 360, 174, 20, 600, cfg.SaidaPx);
            numSaidaPx.Increment = 10;
            pa.Controls.Add(Tema.Rotulo("Sem clicar: sai do card (uma vez descendo, na outra subindo), espera um instante e volta, como se você tirasse o mouse e pusesse de novo. 0 ms = desligado.", 0, 202, 456, 34, 9, Tema.Sub));
            chkAuto = Tema.Caixa(pa, "Parar sozinho quando entrar no time", 0, 244, cfg.AutoParar);
            pa.Controls.Add(Tema.Rotulo("Quanto da tela precisa mudar (%)", 0, 280, 330, 22, 11));
            numLimiar = Tema.Numero(pa, 360, 278, 20, 95, cfg.Limiar);
            pa.Controls.Add(Tema.Rotulo("Conferências seguidas antes de parar (1 por s)", 0, 316, 350, 22, 11));
            numChecagens = Tema.Numero(pa, 360, 314, 1, 30, cfg.Checagens);
            chkTopo = Tema.Caixa(pa, "Janela sempre por cima do jogo", 0, 358, cfg.SempreNoTopo);
            chkBipe = Tema.Caixa(pa, "Bipe ao entrar no time", 0, 390, cfg.Bipe);
            var btnSalvar = Tema.Botao("Salvar ajustes", 0, 436, 456, 44, Tema.Oliva);
            lblSalvo = Tema.Rotulo("", 0, 488, 456, 20, 10, Tema.Verde);
            pa.Controls.AddRange(new Control[] { btnSalvar, lblSalvo });

            btnSalvar.Click += (s, e) =>
            {
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
            };
        }

        void SalvarConfig()
        {
            try
            {
                cfg.Salvar();
                lblSalvo.ForeColor = Tema.Verde;
                lblSalvo.Text = "Ajustes salvos.";
                Log("Ajustes salvos");
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
            pu.Controls.Add(Tema.Rotulo($"Versão instalada: v{Updater.VersaoTexto}", 0, 0, 456, 26, 13, Tema.Texto, true));
            var link = new LinkLabel
            {
                Text = "Ver o projeto no GitHub", Bounds = new Rectangle(0, 30, 456, 20), Font = Tema.Fonte(10),
                LinkColor = Tema.Destaque, ActiveLinkColor = Tema.Texto, BackColor = Color.Transparent
            };
            link.LinkClicked += (s, e) => Process.Start(Updater.Repositorio);
            chkVerificar = Tema.Caixa(pu, "Verificar atualizações ao abrir", 0, 56, cfg.VerificarAoAbrir);
            btnVerificar = Tema.Botao("Verificar agora", 0, 92, 222, 44, Tema.Oliva);
            btnInstalar = Tema.Botao("Instalar atualização", 234, 92, 222, 44, Color.FromArgb(150, 112, 24));
            btnInstalar.Enabled = false;
            lblAtual = Tema.Rotulo("Ainda não verificado.", 0, 142, 456, 36, 10, Tema.Sub);

            var placa = new PlacaMetal { Bounds = new Rectangle(0, 206, 456, 358) };
            rtb = new RichTextBox
            {
                Bounds = new Rectangle(12, 10, 432, 338), ReadOnly = true, BorderStyle = BorderStyle.None,
                BackColor = FundoLista, ForeColor = Tema.Texto, Font = Tema.Fonte(11)
            };
            placa.Controls.Add(rtb);
            pu.Controls.AddRange(new Control[] { link, btnVerificar, btnInstalar, lblAtual, Tema.Rotulo("NOVIDADES", 0, 180, 456, 22, 12, Tema.Destaque, true), placa });

            btnVerificar.Click += (s, e) => VerificarAtualizacao(false);
            btnInstalar.Click += (s, e) => InstalarAtualizacao();
            chkVerificar.CheckedChanged += (s, e) =>
            {
                cfg.VerificarAoAbrir = chkVerificar.Checked;
                try { cfg.Salvar(); } catch { }
            };

            try { MostrarNovidades(Updater.NovidadesLocais()); } catch { }
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
                    Log($"Atualização v{info.Versao} disponível - veja a aba Atualizações");
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
            if (remoto == null) return;
            Parar();
            btnInstalar.Enabled = btnVerificar.Enabled = false;
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
                btnInstalar.Enabled = btnVerificar.Enabled = true;
            }
        }
    }
}
