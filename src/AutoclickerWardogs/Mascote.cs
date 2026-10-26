using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace AutoclickerWardogs
{
    enum Humor { Parado, Clicando, Pausado, Feliz }

    // Cachorro de guerra doido: capacete do exército, olho arregalado, cicatriz e plaqueta.
    // Desenhado em vetor num quadro de 100 x 100, então serve para qualquer tamanho (inclusive o ícone).
    class Mascote : Control
    {
        readonly Timer animacao = new Timer { Interval = 40 };
        readonly Random rnd = new Random();
        Humor humor = Humor.Parado;
        float fase;

        public Humor Humor { get => humor; set { humor = value; Invalidate(); } }

        public Mascote()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
            animacao.Tick += (s, e) => { fase += 0.25f; Invalidate(); };
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            animacao.Enabled = Visible;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) animacao.Dispose();
            base.Dispose(disposing);
        }

        // deixa arrastar a janela pelo cachorro
        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84, HTTRANSPARENT = -1;
            if (m.Msg == WM_NCHITTEST) { m.Result = (IntPtr)HTTRANSPARENT; return; }
            base.WndProc(ref m);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            float lado = Math.Min(Width, Height);
            float dx = 0, dy = 0, giro = 0;
            switch (humor)
            {
                case Humor.Clicando: // tremendo de raiva
                    dx = rnd.Next(-15, 16) / 10f; dy = rnd.Next(-10, 11) / 10f; giro = (float)Math.Sin(fase * 2.3) * 2.5f;
                    break;
                case Humor.Feliz:    // pulando de alegria
                    dy = -(float)Math.Abs(Math.Sin(fase * 0.9)) * 5;
                    break;
                default:             // respirando
                    dy = (float)Math.Sin(fase * 0.35) * 0.8f;
                    break;
            }
            var g = e.Graphics;
            g.TranslateTransform((Width - lado) / 2, (Height - lado) / 2);
            g.ScaleTransform(lado / 100f, lado / 100f);
            g.TranslateTransform(50 + dx, 50 + dy);
            g.RotateTransform(giro);
            g.TranslateTransform(-50, -50);
            Desenhar(g, humor, fase);
        }

        static readonly Color Contorno = Color.FromArgb(22, 20, 14);
        static readonly Color PeloClaro = Color.FromArgb(176, 132, 82);
        static readonly Color PeloEscuro = Color.FromArgb(112, 78, 44);
        static readonly Color Focinho = Color.FromArgb(214, 190, 150);
        static readonly Color CapaceteClaro = Color.FromArgb(104, 118, 60);
        static readonly Color CapaceteEscuro = Color.FromArgb(58, 66, 32);

        public static void Desenhar(Graphics g, Humor humor, float fase)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var contorno = new Pen(Contorno, 2.2f) { LineJoin = LineJoin.Round })
            {
                // orelhas caídas (a direita rasgada de batalha)
                using (var orelha = new SolidBrush(PeloEscuro))
                {
                    var esq = new[] { new PointF(15, 38), new PointF(3, 64), new PointF(12, 66), new PointF(22, 48) };
                    var dir = new[] { new PointF(85, 38), new PointF(97, 62), new PointF(92, 61), new PointF(93, 66), new PointF(86, 66), new PointF(78, 48) };
                    g.FillPolygon(orelha, esq); g.DrawPolygon(contorno, esq);
                    g.FillPolygon(orelha, dir); g.DrawPolygon(contorno, dir);
                }

                // cara de buldogue
                var cara = new[]
                {
                    new PointF(15, 36), new PointF(85, 36), new PointF(89, 58), new PointF(81, 76),
                    new PointF(66, 88), new PointF(50, 92), new PointF(34, 88), new PointF(19, 76), new PointF(11, 58)
                };
                using (var pelo = new LinearGradientBrush(new RectangleF(10, 36, 80, 56), PeloClaro, PeloEscuro, 90f))
                    g.FillPolygon(pelo, cara);
                using (var claro = new SolidBrush(Focinho))
                    g.FillEllipse(claro, 29, 58, 42, 31);
                g.DrawPolygon(contorno, cara);

                // cicatriz no olho direito, com pontos
                using (var cicatriz = new Pen(Color.FromArgb(150, 40, 34), 2f))
                using (var ponto = new Pen(Color.FromArgb(70, 30, 26), 1.2f))
                {
                    g.DrawLine(cicatriz, 61, 39, 73, 60);
                    for (int i = 0; i < 4; i++)
                    {
                        float t = 0.2f + i * 0.2f, x = 61 + 12 * t, y = 39 + 21 * t;
                        g.DrawLine(ponto, x - 2.5f, y + 1.4f, x + 2.5f, y - 1.4f);
                    }
                }

                DesenharOlhos(g, contorno, humor, fase);

                // nariz
                using (var nariz = new SolidBrush(Color.FromArgb(28, 24, 22)))
                    g.FillPolygon(nariz, new[] { new PointF(43, 60), new PointF(57, 60), new PointF(54, 66), new PointF(50, 68), new PointF(46, 66) });
                using (var brilho = new SolidBrush(Color.FromArgb(120, 255, 255, 255)))
                    g.FillEllipse(brilho, 46, 61, 4, 2);

                // boca aberta com dentes e presas
                bool feliz = humor == Humor.Feliz;
                var boca = feliz
                    ? new[] { new PointF(35, 72), new PointF(65, 72), new PointF(60, 85), new PointF(40, 85) }
                    : new[] { new PointF(37, 73), new PointF(63, 73), new PointF(59, 82), new PointF(41, 82) };
                using (var escuro = new SolidBrush(Color.FromArgb(46, 14, 16)))
                    g.FillPolygon(escuro, boca);

                // língua para fora, balançando enquanto clica
                float ang = humor == Humor.Clicando ? (float)Math.Sin(fase * 3) * 18 : feliz ? (float)Math.Sin(fase) * 8 : 0;
                var estado = g.Save();
                g.TranslateTransform(45, 80);
                g.RotateTransform(ang);
                using (var lingua = new SolidBrush(Color.FromArgb(232, 92, 120)))
                using (var meio = new Pen(Color.FromArgb(170, 50, 76), 1.2f))
                {
                    g.FillEllipse(lingua, -6, -2, 12, feliz ? 20 : 16);
                    g.DrawLine(meio, 0, 1, 0, feliz ? 14 : 11);
                }
                g.Restore(estado);

                using (var dente = new SolidBrush(Color.FromArgb(246, 242, 228)))
                {
                    float topo = boca[0].Y;
                    for (int i = 0; i < 5; i++)
                    {
                        float x = 42 + i * 4;
                        g.FillPolygon(dente, new[] { new PointF(x, topo), new PointF(x + 3.2f, topo), new PointF(x + 1.6f, topo + 3) });
                    }
                    // presas
                    g.FillPolygon(dente, new[] { new PointF(boca[0].X + 1, topo), new PointF(boca[0].X + 5, topo), new PointF(boca[0].X + 3, topo + 7) });
                    g.FillPolygon(dente, new[] { new PointF(boca[1].X - 5, topo), new PointF(boca[1].X - 1, topo), new PointF(boca[1].X - 3, topo + 7) });
                }
                g.DrawPolygon(contorno, boca);

                DesenharCapacete(g, contorno);

                // plaqueta de identificação
                using (var corrente = new Pen(Color.FromArgb(170, 170, 160), 1.2f) { DashStyle = DashStyle.Dot })
                    g.DrawBezier(corrente, 33, 87, 40, 95, 46, 95, 50, 93);
                using (var placa = new LinearGradientBrush(new RectangleF(46, 91, 11, 9), Color.FromArgb(222, 222, 212), Color.FromArgb(140, 140, 130), 45f))
                    g.FillRectangle(placa, 46, 91, 11, 8);
                using (var risco = new Pen(Color.FromArgb(90, 90, 84), 0.8f))
                {
                    g.DrawLine(risco, 48, 93.5f, 55, 93.5f);
                    g.DrawLine(risco, 48, 96, 53, 96);
                }

                if (humor == Humor.Pausado) Zzz(g, fase);
                if (humor == Humor.Clicando) Raiva(g, fase);
            }
        }

        static void DesenharOlhos(Graphics g, Pen contorno, Humor humor, float fase)
        {
            if (humor == Humor.Feliz)
            {
                using (var p = new Pen(Contorno, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawArc(p, 27, 46, 14, 10, 200, 140);
                    g.DrawArc(p, 59, 46, 14, 10, 200, 140);
                }
                return;
            }

            using (var branco = new SolidBrush(Color.FromArgb(250, 246, 234)))
            using (var pupila = new SolidBrush(Color.FromArgb(18, 16, 14)))
            {
                // olho esquerdo arregalado, doido
                g.FillEllipse(branco, 25, 41, 18, 18);
                if (humor == Humor.Clicando)
                    using (var veia = new Pen(Color.FromArgb(200, 210, 40, 40), 0.9f))
                    {
                        g.DrawLine(veia, 26, 50, 30, 49); g.DrawLine(veia, 42, 47, 38, 49); g.DrawLine(veia, 34, 58, 34, 55);
                    }
                float px = 34, py = 50;
                if (humor == Humor.Clicando) { px += (float)Math.Cos(fase * 2) * 3.5f; py += (float)Math.Sin(fase * 2) * 3.5f; }
                float rp = humor == Humor.Clicando ? 2.2f : 3.4f;
                g.FillEllipse(pupila, px - rp, py - rp, rp * 2, rp * 2);
                g.DrawEllipse(contorno, 25, 41, 18, 18);

                // olho direito apertado, bravo
                var dir = new[] { new PointF(58, 49), new PointF(75, 46), new PointF(73, 54), new PointF(60, 55) };
                g.FillPolygon(branco, dir);
                g.FillEllipse(pupila, 64, 48.5f, 5, 5);
                g.DrawPolygon(contorno, dir);
            }

            // sobrancelhas: uma lá em cima (doida) e outra franzida (brava)
            using (var p = new Pen(Contorno, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(p, 24, 39, 40, 36);
                g.DrawLine(p, 57, 45, 76, 41);
            }

            if (humor == Humor.Pausado)
                using (var palpebra = new SolidBrush(PeloEscuro))
                using (var olhoDir = new GraphicsPath())
                {
                    // pálpebras caídas, cobrindo a metade de cima de cada olho
                    g.FillPie(palpebra, 25, 41, 18, 18, 180, 180);
                    g.DrawLine(contorno, 25, 50, 43, 50);
                    olhoDir.AddPolygon(new[] { new PointF(58, 49), new PointF(75, 46), new PointF(73, 54), new PointF(60, 55) });
                    var estado = g.Save();
                    g.SetClip(olhoDir);
                    g.FillRectangle(palpebra, 56, 44, 22, 7);
                    g.Restore(estado);
                    g.DrawLine(contorno, 58.5f, 51, 74.5f, 49);
                }
        }

        static void DesenharCapacete(Graphics g, Pen contorno)
        {
            using (var casco = new GraphicsPath())
            {
                casco.AddArc(10, 2, 80, 64, 180, 180);
                casco.CloseFigure();
                using (var b = new LinearGradientBrush(new RectangleF(10, 2, 80, 36), CapaceteClaro, CapaceteEscuro, 100f))
                    g.FillPath(b, casco);
                // rede de camuflagem
                using (var rede = new Pen(Color.FromArgb(70, 30, 34, 16), 1f))
                {
                    var estado = g.Save();
                    g.SetClip(casco);
                    for (int i = -40; i < 100; i += 9)
                    {
                        g.DrawLine(rede, i, 2, i + 34, 36);
                        g.DrawLine(rede, i + 34, 2, i, 36);
                    }
                    g.Restore(estado);
                }
                g.DrawPath(contorno, casco);
            }
            // aba do capacete
            var aba = new[] { new PointF(5, 33), new PointF(95, 33), new PointF(91, 40), new PointF(9, 40) };
            using (var b = new SolidBrush(CapaceteEscuro))
                g.FillPolygon(b, aba);
            g.DrawPolygon(contorno, aba);
            // estrela do exército
            Estrela(g, 50, 19, 8, Color.FromArgb(232, 226, 200));
            // buraco de bala
            using (var furo = new SolidBrush(Color.FromArgb(24, 22, 14)))
            using (var borda = new Pen(Color.FromArgb(140, 150, 90), 0.8f))
            {
                g.FillEllipse(furo, 72, 18, 4, 4);
                g.DrawEllipse(borda, 71.5f, 17.5f, 5, 5);
            }
        }

        static void Estrela(Graphics g, float cx, float cy, float r, Color cor)
        {
            var pts = new PointF[10];
            for (int i = 0; i < 10; i++)
            {
                double a = -Math.PI / 2 + i * Math.PI / 5;
                float raio = i % 2 == 0 ? r : r * 0.42f;
                pts[i] = new PointF(cx + (float)Math.Cos(a) * raio, cy + (float)Math.Sin(a) * raio);
            }
            using (var b = new SolidBrush(cor))
                g.FillPolygon(b, pts);
        }

        static void Zzz(Graphics g, float fase)
        {
            using (var f = new Font("Arial Black", 9f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var b = new SolidBrush(Color.FromArgb(220, Tema.Amarelo)))
            {
                float sobe = (fase * 2) % 12;
                g.DrawString("z", f, b, 84, 22 - sobe);
                g.DrawString("Z", f, b, 90, 12 - sobe / 2);
            }
        }

        static void Raiva(Graphics g, float fase)
        {
            // marca de raiva de desenho animado piscando
            if ((int)(fase * 2) % 2 != 0) return;
            using (var p = new Pen(Tema.Vermelho, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawArc(p, 82, 2, 8, 8, 90, 90);
                g.DrawArc(p, 90, 2, 8, 8, 0, 90);
                g.DrawArc(p, 82, 10, 8, 8, 180, 90);
                g.DrawArc(p, 90, 10, 8, 8, 270, 90);
            }
        }
    }
}
