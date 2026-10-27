using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AutoclickerWardogs
{
    // Janela "Nova versão disponível!" no visual de guerra.
    // Devolve DialogResult.OK quando a pessoa escolhe atualizar agora.
    class AvisoAtualizacao : Form
    {
        public AvisoAtualizacao(InfoAtualizacao info)
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Nova versão do Autoclicker Wardogs";
            ClientSize = new Size(520, 400);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Tema.Fundo;
            DoubleBuffered = true;
            KeyPreview = true;

            var mascote = new Mascote { Bounds = new Rectangle(18, 22, 110, 110), Humor = Humor.Feliz };
            var titulo = new Estencil
            {
                Text = "NOVA VERSÃO DISPONÍVEL!", Bounds = new Rectangle(140, 30, 370, 40),
                Font = Tema.Fonte(20, true), ForeColor = Tema.Destaque
            };
            var versoes = Tema.Rotulo($"Você está na v{Updater.VersaoTexto}  ▸  nova: v{info.Versao}", 142, 72, 360, 24, 12, Tema.Texto, true);
            var dica = Tema.Rotulo("Leva poucos segundos: o programa baixa, troca o arquivo e abre de novo sozinho.", 142, 98, 360, 40, 9.5f, Tema.Sub);

            var placa = new PlacaMetal { Bounds = new Rectangle(18, 146, 484, 168) };
            var lista = new RichTextBox
            {
                Bounds = new Rectangle(12, 10, 460, 148), ReadOnly = true, BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(30, 34, 24), ForeColor = Tema.Texto, Font = Tema.Fonte(10.5f),
                ScrollBars = RichTextBoxScrollBars.Vertical, TabStop = false
            };
            placa.Controls.Add(lista);
            PreencherNovidades(lista, info);

            var atualizar = Tema.Botao("Atualizar agora", 18, 330, 300, 50, Color.FromArgb(150, 112, 24));
            var depois = Tema.Botao("Depois", 330, 330, 172, 50, Tema.Oliva);
            atualizar.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            depois.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[] { mascote, titulo, versoes, dica, placa, atualizar, depois });
            ResumeLayout(false);
            Shown += (s, e) => atualizar.Focus();
        }

        static void PreencherNovidades(RichTextBox rtb, InfoAtualizacao info)
        {
            var novas = (info.Novidades ?? new Novidade[0])
                .Where(n => Version.TryParse(n.Versao, out var v) && v > Updater.Versao)
                .ToArray();
            if (novas.Length == 0)
            {
                rtb.Text = "Melhorias e correções.";
                return;
            }
            foreach (var n in novas)
            {
                rtb.SelectionIndent = 0;
                rtb.SelectionHangingIndent = 0;
                rtb.SelectionFont = Tema.Fonte(11, true);
                rtb.SelectionColor = Tema.Verde;
                rtb.AppendText($"v{n.Versao}   {n.Data}\n");
                rtb.SelectionIndent = 8;
                rtb.SelectionHangingIndent = 15;
                rtb.SelectionFont = Tema.Fonte(10.5f);
                rtb.SelectionColor = Tema.Texto;
                foreach (var item in n.Itens ?? new string[0]) rtb.AppendText($"▸  {item}\n");
            }
            rtb.SelectionStart = 0;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x20000;
                var cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); }
            base.OnKeyDown(e);
        }

        // arrastar pela área livre
        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84, HTCLIENT = 1, HTCAPTION = 2;
            base.WndProc(ref m);
            if (m.Msg == WM_NCHITTEST && (int)m.Result == HTCLIENT) m.Result = (IntPtr)HTCAPTION;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            float s = DeviceDpi / 96f;
            using (var camo = new TextureBrush(Tema.Camuflagem))
                g.FillRectangle(camo, ClientRectangle);
            Tema.FaixaAlerta(g, new RectangleF(0, 0, Width, 8 * s));
            using (var p = new Pen(Tema.Destaque, 2 * s))
                g.DrawRectangle(p, s, s, Width - 2 * s, Height - 2 * s);
        }
    }
}
