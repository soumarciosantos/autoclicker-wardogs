using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace AutoclickerWardogs
{
    // Linguagem Wardogs: central tática limpa, sem camuflagem, rebites ou mascote.
    // Os métodos legados ficam disponíveis para controles antigos
    // que ainda fazem parte do projeto, mas não são usados nas telas principais.
    static class Tema
    {
        public static readonly Color Fundo = Color.FromArgb(16, 22, 21);
        public static readonly Color Lateral = Color.FromArgb(22, 30, 27);
        public static readonly Color Painel = Color.FromArgb(28, 38, 35);
        public static readonly Color Borda = Color.FromArgb(54, 70, 62);
        public static readonly Color Rebite = Color.FromArgb(99, 115, 96);
        public static readonly Color Grade = Color.FromArgb(48, 62, 55);
        public static readonly Color Texto = Color.FromArgb(243, 244, 232);
        public static readonly Color Sub = Color.FromArgb(173, 188, 174);
        public static readonly Color Destaque = Color.FromArgb(239, 185, 78);
        public static readonly Color Oliva = Color.FromArgb(101, 130, 76);
        public static readonly Color Verde = Color.FromArgb(117, 210, 159);
        public static readonly Color Amarelo = Color.FromArgb(239, 185, 78);
        public static readonly Color Vermelho = Color.FromArgb(222, 105, 90);
        public static readonly Color Azul = Color.FromArgb(96, 181, 201);

        public static Font Fonte(float tamanho, bool negrito = false) =>
            new Font("Segoe UI", tamanho, negrito ? FontStyle.Bold : FontStyle.Regular);

        public static Font FonteMono(float tamanho) => new Font("Consolas", tamanho);

        public static Color ComAlfa(Color c, int alfa) => Color.FromArgb(alfa, c);

        // retângulo com dois cantos cortados, como placa de blindagem
        public static GraphicsPath Chanfro(RectangleF r, float corte)
        {
            var p = new GraphicsPath();
            p.AddPolygon(new[]
            {
                new PointF(r.X + corte, r.Y), new PointF(r.Right, r.Y),
                new PointF(r.Right, r.Bottom - corte), new PointF(r.Right - corte, r.Bottom),
                new PointF(r.X, r.Bottom), new PointF(r.X, r.Y + corte)
            });
            return p;
        }

        public static GraphicsPath Arredondado(RectangleF r, float raio)
        {
            var p = new GraphicsPath();
            float d = Math.Min(Math.Min(r.Width, r.Height), raio * 2);
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        // ---------- camuflagem digital (gerada uma vez e reaproveitada) ----------
        static Bitmap camuflagem;
        public static Bitmap Camuflagem
        {
            get
            {
                if (camuflagem != null) return camuflagem;
                Color[] tons =
                {
                    Color.FromArgb(17, 19, 14), Color.FromArgb(23, 26, 18),
                    Color.FromArgb(29, 32, 22), Color.FromArgb(20, 22, 16)
                };
                const int bloco = 6, w = 120, h = 110;
                var rnd = new Random(7);
                // ruído em grade grossa, suavizado e quantizado em blocos (estilo "pixel" militar)
                var ruido = new double[w / 4 + 2, h / 4 + 2];
                for (int i = 0; i < ruido.GetLength(0); i++)
                    for (int j = 0; j < ruido.GetLength(1); j++)
                        ruido[i, j] = rnd.NextDouble();
                var bmp = new Bitmap(w * bloco, h * bloco);
                using (var g = Graphics.FromImage(bmp))
                    for (int x = 0; x < w; x++)
                        for (int y = 0; y < h; y++)
                        {
                            double fx = x / 4.0, fy = y / 4.0;
                            int ix = (int)fx, iy = (int)fy;
                            double tx = fx - ix, ty = fy - iy;
                            double v = ruido[ix, iy] * (1 - tx) * (1 - ty) + ruido[ix + 1, iy] * tx * (1 - ty)
                                     + ruido[ix, iy + 1] * (1 - tx) * ty + ruido[ix + 1, iy + 1] * tx * ty;
                            v += (rnd.NextDouble() - 0.5) * 0.18;
                            int t = v < 0.32 ? 0 : v < 0.5 ? 3 : v < 0.68 ? 1 : 2;
                            using (var b = new SolidBrush(tons[t]))
                                g.FillRectangle(b, x * bloco, y * bloco, bloco, bloco);
                        }
                return camuflagem = bmp;
            }
        }

        // faixa de alerta amarela e preta
        public static void FaixaAlerta(Graphics g, RectangleF r)
        {
            var estado = g.Save();
            g.SetClip(r);
            using (var preto = new SolidBrush(Color.FromArgb(20, 20, 16)))
                g.FillRectangle(preto, r);
            using (var amarelo = new SolidBrush(Amarelo))
                for (float x = r.X - r.Height; x < r.Right + r.Height; x += r.Height * 1.6f)
                    g.FillPolygon(amarelo, new[]
                    {
                        new PointF(x, r.Bottom), new PointF(x + r.Height * 0.8f, r.Bottom),
                        new PointF(x + r.Height * 1.6f, r.Y), new PointF(x + r.Height * 0.8f, r.Y)
                    });
            g.Restore(estado);
        }

        // ---------- fábricas dos controles ----------
        public static Label Rotulo(string texto, int x, int y, int w, int h, float tamanho = 9, Color? cor = null, bool negrito = false) =>
            new Label
            {
                Text = texto, Bounds = new Rectangle(x, y, w, h), ForeColor = cor ?? Texto,
                Font = Fonte(tamanho, negrito), BackColor = Color.Transparent,
                AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft
            };

        public static BotaoMilitar Botao(string texto, int x, int y, int w, int h, Color cor) =>
            new BotaoMilitar { Text = texto, Bounds = new Rectangle(x, y, w, h), Cor = cor };

        // cartão com título pequeno e valor grande; devolve o rótulo do valor
        public static Label Cartao(Control pai, string titulo, int x, int y)
        {
            var c = new PlacaMetal { Bounds = new Rectangle(x, y, 174, 82) };
            c.Controls.Add(Rotulo(titulo, 16, 12, 148, 18, 8.5f, Sub));
            var valor = Rotulo("0", 16, 33, 148, 38, 20, Texto, true);
            c.Controls.Add(valor);
            pai.Controls.Add(c);
            return valor;
        }

        public static NumericUpDown Numero(Control pai, int x, int y, int min, int max, int valor)
        {
            var n = new NumericUpDown
            {
                Bounds = new Rectangle(x, y, 80, 28), Minimum = min, Maximum = max,
                BackColor = Painel, ForeColor = Texto, BorderStyle = BorderStyle.FixedSingle,
                Font = Fonte(11, true), TextAlign = HorizontalAlignment.Center
            };
            n.Value = Math.Max(min, Math.Min(max, valor));
            pai.Controls.Add(n);
            return n;
        }

        public static Alternador Caixa(Control pai, string texto, int x, int y, bool marcado)
        {
            var c = new Alternador { Text = texto, Bounds = new Rectangle(x, y, 320, 32), Checked = marcado };
            pai.Controls.Add(c);
            return c;
        }
    }
}
