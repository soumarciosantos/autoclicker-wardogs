using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
        readonly Dictionary<string, Panel> paginas = new Dictionary<string, Panel>();
        readonly Dictionary<string, Button> abas = new Dictionary<string, Button>();
        readonly Timer tTeclas = new Timer { Interval = 40 };
        readonly Timer tStatus = new Timer { Interval = 250 };
        readonly Timer tSegundo = new Timer { Interval = 1000 };
        readonly Timer tContagem = new Timer { Interval = 1000 };
        readonly bool atualizado;

        Point alvo;
        Color[] referencia;
        int trocada, contagem;
        double frac, cps;
        long ultimoTotal;
        bool prevF8, prevF9, novaSessao = true;
        InfoAtualizacao remoto;

        Label lblStatus, lblDica, lblTotal, cardAgora, cardMedia, cardTempo, cardTela, lblAlvo, lblSalvo, lblAtual;
        Grafico grafico;
        Button btnIniciar, btnParar, btnVerificar, btnInstalar;
        ListBox log;
        RichTextBox rtb;
        NumericUpDown numIntervalo, numTremor, numSaidaMs, numSaidaPx, numLimiar, numChecagens;
        CheckBox chkAuto, chkTopo, chkBipe, chkVerificar;

        public MainForm(bool atualizado)
        {
            this.atualizado = atualizado;
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = $"Autoclicker Wardogs v{Updater.VersaoTexto}";
            ClientSize = new Size(480, 600);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Tema.Fundo;
            ForeColor = Tema.Texto;
            TopMost = cfg.SempreNoTopo;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            // abas feitas com botões (o TabControl padrão não aceita tema escuro)
            string[] nomes = { "Painel", "Ajustes", "Atualizações" };
            for (int i = 0; i < nomes.Length; i++)
            {
                string nome = nomes[i];
                var b = Tema.Botao(nome, 12 + i * 152, 12, 148, 34, Tema.Painel);
                b.Click += (s, e) => MostrarPagina(nome);
                Controls.Add(b);
                abas[nome] = b;
                var p = new Panel { Bounds = new Rectangle(12, 56, 456, 532), BackColor = Tema.Fundo, Visible = false };
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
                abas[k].BackColor = k == nome ? Tema.Azul : Tema.Painel;
            }
        }

        // ---------- Painel ----------
        void MontarPainel(Panel pp)
        {
            lblStatus = Tema.Rotulo("PARADO", 0, 0, 456, 34, 18, Tema.Sub, true);
            lblDica = Tema.Rotulo("Coloque o mouse no card do Wardogs e aperte F8.", 0, 34, 456, 20, 9, Tema.Sub);
            lblTotal = Tema.Rotulo("0", 0, 58, 456, 62, 36, Tema.Texto, true);
            pp.Controls.AddRange(new Control[] { lblStatus, lblDica, lblTotal, Tema.Rotulo("cliques nesta sessão", 4, 118, 300, 18, 9, Tema.Sub) });

            cardAgora = Tema.Cartao(pp, "AGORA /s", 0, 144);
            cardMedia = Tema.Cartao(pp, "MÉDIA /s", 116, 144);
            cardTempo = Tema.Cartao(pp, "TEMPO", 232, 144);
            cardTela = Tema.Cartao(pp, "TELA MUDOU", 348, 144);

            lblAlvo = Tema.Rotulo("Alvo: nenhum", 0, 208, 456, 20, 9, Tema.Sub);
            grafico = new Grafico { Bounds = new Rectangle(0, 232, 456, 120) };
            btnIniciar = Tema.Botao("Iniciar (F8)", 0, 362, 222, 42, Tema.Verde);
            btnParar = Tema.Botao("Parar (F9)", 234, 362, 222, 42, Tema.Vermelho);
            log = new ListBox
            {
                Bounds = new Rectangle(0, 414, 456, 118), BackColor = Tema.Painel, ForeColor = Tema.Texto,
                BorderStyle = BorderStyle.None, Font = new Font("Consolas", 9), HorizontalScrollbar = true
            };
            pp.Controls.AddRange(new Control[] { lblAlvo, grafico, btnIniciar, btnParar, log });

            // botão Iniciar: contagem de 3 s para dar tempo de levar o mouse até o card
            btnIniciar.Click += (s, e) =>
            {
                if (clicker.Rodando) { Pausar(); return; }
                contagem = 3;
                Status("INICIANDO EM 3...", Tema.Amarelo, "Leve o mouse até o card do time.");
                tContagem.Start();
            };
            btnParar.Click += (s, e) => Parar();
        }

        void Log(string msg)
        {
            log.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}");
            while (log.Items.Count > 200) log.Items.RemoveAt(200);
        }

        void Status(string texto, Color cor, string dica = null)
        {
            lblStatus.Text = texto;
            lblStatus.ForeColor = cor;
            if (dica != null) lblDica.Text = dica;
        }

        void PassoContagem()
        {
            contagem--;
            if (contagem <= 0) { tContagem.Stop(); Iniciar(); }
            else Status($"INICIANDO EM {contagem}...", Tema.Amarelo, "Leve o mouse até o card do time.");
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
                Status("PARADO", Tema.Sub, "O mouse está em cima desta janela. Leve até o card do time e aperte F8.");
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
            btnIniciar.BackColor = Tema.Amarelo;
            Status("CLICANDO", Tema.Verde, "F8 pausa  |  F9 para");
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
            btnIniciar.BackColor = Tema.Verde;
            Status("PAUSADO", Tema.Amarelo, "F8 continua no ponto atual do mouse  |  F9 encerra a sessão");
            Log($"Pausado - {clicker.Total:N0} cliques");
        }

        void Parar(bool entrou = false)
        {
            CancelarContagem();
            bool tinhaSessao = clicker.Rodando || sessao.ElapsedTicks > 0;
            clicker.Parar(); sessao.Stop(); novaSessao = true;
            btnIniciar.Text = "Iniciar (F8)";
            btnIniciar.BackColor = Tema.Verde;
            if (entrou)
            {
                Status("ENTROU NO TIME!", Tema.Azul, "A tela de facção sumiu. Aperte F8 para uma nova sessão.");
                if (cfg.Bipe) Task.Run(() => { Console.Beep(1000, 250); Console.Beep(1400, 250); });
                Activate();
            }
            else
            {
                Status("PARADO", Tema.Sub, "Coloque o mouse no card do Wardogs e aperte F8.");
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
            pa.Controls.Add(Tema.Rotulo("Intervalo entre cliques (ms)", 0, 4, 300, 22, 10));
            numIntervalo = Tema.Numero(pa, 340, 2, 0, 1000, cfg.IntervaloMs);
            pa.Controls.Add(Tema.Rotulo("Recomendado: 15 ms (≈ 60/s). O jogo só lê um clique por quadro; abaixo de 10 ms ele começa a perder cliques e movimentos.", 0, 30, 456, 34, 8, Tema.Sub));
            pa.Controls.Add(Tema.Rotulo("Mexer o mouse entre cliques (px)", 0, 72, 300, 22, 10));
            numTremor = Tema.Numero(pa, 340, 70, 0, 10, cfg.Tremor);
            pa.Controls.Add(Tema.Rotulo("Anda esse tanto para a esquerda e para a direita a cada clique, para o jogo perceber o mouse no card. 0 = parado.", 0, 98, 456, 34, 8, Tema.Sub));
            pa.Controls.Add(Tema.Rotulo("Sair do card e voltar a cada (ms)", 0, 140, 300, 22, 10));
            numSaidaMs = Tema.Numero(pa, 340, 138, 0, 10000, cfg.SaidaMs);
            numSaidaMs.Increment = 100;
            pa.Controls.Add(Tema.Rotulo("Distância que o mouse anda (px)", 0, 176, 300, 22, 10));
            numSaidaPx = Tema.Numero(pa, 340, 174, 20, 600, cfg.SaidaPx);
            numSaidaPx.Increment = 10;
            pa.Controls.Add(Tema.Rotulo("Sem clicar: sai do card (uma vez descendo, na outra subindo), espera um instante e volta, como se você tirasse o mouse e pusesse de novo. 0 ms = desligado.", 0, 202, 456, 34, 8, Tema.Sub));
            chkAuto = Tema.Caixa(pa, "Parar sozinho quando entrar no time", 0, 244, cfg.AutoParar);
            pa.Controls.Add(Tema.Rotulo("Quanto da tela precisa mudar (%)", 0, 280, 300, 22, 10));
            numLimiar = Tema.Numero(pa, 340, 278, 20, 95, cfg.Limiar);
            pa.Controls.Add(Tema.Rotulo("Conferências seguidas antes de parar (1 por s)", 0, 316, 330, 22, 10));
            numChecagens = Tema.Numero(pa, 340, 314, 1, 30, cfg.Checagens);
            chkTopo = Tema.Caixa(pa, "Janela sempre por cima do jogo", 0, 358, cfg.SempreNoTopo);
            chkBipe = Tema.Caixa(pa, "Bipe ao entrar no time", 0, 388, cfg.Bipe);
            var btnSalvar = Tema.Botao("Salvar ajustes", 0, 432, 456, 40, Tema.Azul);
            lblSalvo = Tema.Rotulo("", 0, 478, 456, 20, 9, Tema.Verde);
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
                clicker.DelayMs = cfg.IntervaloMs;
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
            pu.Controls.Add(Tema.Rotulo($"Versão instalada: v{Updater.VersaoTexto}", 0, 0, 456, 26, 12, Tema.Texto, true));
            var link = new LinkLabel
            {
                Text = "Ver o projeto no GitHub", Bounds = new Rectangle(0, 30, 456, 20), Font = Tema.Fonte(9),
                LinkColor = Tema.Azul, ActiveLinkColor = Tema.Texto, BackColor = Color.Transparent
            };
            link.LinkClicked += (s, e) => Process.Start(Updater.Repositorio);
            chkVerificar = Tema.Caixa(pu, "Verificar atualizações ao abrir", 0, 56, cfg.VerificarAoAbrir);
            btnVerificar = Tema.Botao("Verificar agora", 0, 92, 222, 40, Tema.Azul);
            btnInstalar = Tema.Botao("Instalar atualização", 234, 92, 222, 40, Tema.Verde);
            btnInstalar.Enabled = false;
            lblAtual = Tema.Rotulo("Ainda não verificado.", 0, 140, 456, 36, 9, Tema.Sub);
            rtb = new RichTextBox
            {
                Bounds = new Rectangle(0, 206, 456, 326), ReadOnly = true, BorderStyle = BorderStyle.None,
                BackColor = Tema.Painel, ForeColor = Tema.Texto, Font = Tema.Fonte(10)
            };
            pu.Controls.AddRange(new Control[] { link, btnVerificar, btnInstalar, lblAtual, Tema.Rotulo("Novidades", 0, 180, 456, 22, 11, Tema.Texto, true), rtb });

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
                rtb.SelectionFont = Tema.Fonte(10, true);
                rtb.SelectionColor = nova ? Tema.Verde : Tema.Texto;
                rtb.AppendText($"v{n.Versao}   {n.Data}\n");
                rtb.SelectionFont = Tema.Fonte(10);
                rtb.SelectionColor = Tema.Sub;
                foreach (var item in n.Itens ?? new string[0]) rtb.AppendText($"   •  {item}\n");
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
