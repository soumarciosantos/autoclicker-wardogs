using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AutoclickerWardogs
{
    static class Tema
    {
        public static readonly Color Fundo = Color.FromArgb(18, 18, 20);
        public static readonly Color Painel = Color.FromArgb(32, 32, 36);
        public static readonly Color Grade = Color.FromArgb(48, 48, 54);
        public static readonly Color Texto = Color.FromArgb(232, 232, 232);
        public static readonly Color Sub = Color.FromArgb(150, 150, 156);
        public static readonly Color Verde = Color.FromArgb(56, 170, 96);
        public static readonly Color Amarelo = Color.FromArgb(235, 190, 60);
        public static readonly Color Azul = Color.FromArgb(52, 140, 210);
        public static readonly Color Vermelho = Color.FromArgb(200, 70, 62);

        public static Font Fonte(float tamanho, bool negrito = false) =>
            new Font("Segoe UI", tamanho, negrito ? FontStyle.Bold : FontStyle.Regular);

        public static Label Rotulo(string texto, int x, int y, int w, int h, float tamanho = 9, Color? cor = null, bool negrito = false) =>
            new Label
            {
                Text = texto, Bounds = new Rectangle(x, y, w, h), ForeColor = cor ?? Texto,
                Font = Fonte(tamanho, negrito), BackColor = Color.Transparent
            };

        public static Button Botao(string texto, int x, int y, int w, int h, Color cor)
        {
            var b = new Button
            {
                Text = texto, Bounds = new Rectangle(x, y, w, h), FlatStyle = FlatStyle.Flat,
                BackColor = cor, ForeColor = Color.White, Font = Fonte(10, true), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        // cartão com título pequeno e valor grande; devolve o rótulo do valor
        public static Label Cartao(Control pai, string titulo, int x, int y)
        {
            var c = new Panel { Bounds = new Rectangle(x, y, 108, 58), BackColor = Painel };
            c.Controls.Add(Rotulo(titulo, 8, 6, 96, 16, 8, Sub));
            var valor = Rotulo("0", 8, 22, 96, 30, 14, Texto, true);
            c.Controls.Add(valor);
            pai.Controls.Add(c);
            return valor;
        }

        public static NumericUpDown Numero(Control pai, int x, int y, int min, int max, int valor)
        {
            var n = new NumericUpDown
            {
                Bounds = new Rectangle(x, y, 90, 26), Minimum = min, Maximum = max,
                BackColor = Painel, ForeColor = Texto, BorderStyle = BorderStyle.FixedSingle, Font = Fonte(10)
            };
            n.Value = System.Math.Max(min, System.Math.Min(max, valor));
            pai.Controls.Add(n);
            return n;
        }

        public static CheckBox Caixa(Control pai, string texto, int x, int y, bool marcado)
        {
            var c = new CheckBox
            {
                Text = texto, Bounds = new Rectangle(x, y, 430, 26), Checked = marcado,
                ForeColor = Texto, Font = Fonte(10)
            };
            pai.Controls.Add(c);
            return c;
        }
    }

    // Gráfico de cliques por segundo dos últimos 60 s
    class Grafico : Panel
    {
        public List<double> Dados = new List<double>();

        public Grafico()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Tema.Painel);
            int w = Width, h = Height;

            double max = 10;
            foreach (var v in Dados) if (v > max) max = v;

            using (var fonte = Tema.Fonte(8))
            using (var pincelSub = new SolidBrush(Tema.Sub))
            using (var grade = new Pen(Tema.Grade))
            {
                int topo = fonte.Height + 8;
                foreach (var f in new[] { 0.25, 0.5, 0.75 })
                {
                    int yy = (int)(topo + (h - topo - 4) * f);
                    g.DrawLine(grade, 0, yy, w, yy);
                }
                g.DrawString("cliques/s - últimos 60 s", fonte, pincelSub, 6, 4);
                string txtMax = $"máx {max:N0}";
                g.DrawString(txtMax, fonte, pincelSub, w - g.MeasureString(txtMax, fonte).Width - 6, 4);

                if (Dados.Count < 2) return;
                var pts = new PointF[Dados.Count];
                for (int k = 0; k < Dados.Count; k++)
                {
                    float x = (float)(w - 1 - (Dados.Count - 1 - k) * (w - 2) / 59.0);
                    float y = (float)(h - 4 - Dados[k] / max * (h - topo - 8));
                    pts[k] = new PointF(x, y);
                }
                using (var linha = new Pen(Tema.Verde, 2))
                    g.DrawLines(linha, pts);
            }
        }
    }
}
