using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace AutoclickerWardogs
{
    // base dos controles desenhados à mão: fundo transparente e sem piscar
    abstract class ControleDesenhado : Control
    {
        protected ControleDesenhado()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
        }

        protected float Escala => DeviceDpi / 96f;

        protected static void Rebites(Graphics g, RectangleF r, float margem, float raio)
        {
            using (var b = new SolidBrush(Tema.Rebite))
            using (var brilho = new SolidBrush(Color.FromArgb(60, 255, 255, 230)))
                foreach (var p in new[]
                {
                    new PointF(r.Right - margem, r.Y + margem), new PointF(r.X + margem, r.Bottom - margem)
                })
                {
                    g.FillEllipse(b, p.X - raio, p.Y - raio, raio * 2, raio * 2);
                    g.FillEllipse(brilho, p.X - raio * 0.6f, p.Y - raio * 0.7f, raio * 0.8f, raio * 0.8f);
                }
        }
    }

    // página com fundo de camuflagem
    class Pagina : Panel
    {
        public Pagina()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
            BackColor = Tema.Fundo;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (var t = new TextureBrush(Tema.Camuflagem))
            {
                // alinha a textura com a janela, para a camuflagem continuar sem emenda
                var p = Parent != null ? Parent.PointToClient(PointToScreen(Point.Empty)) : Point.Empty;
                t.TranslateTransform(-p.X, -p.Y);
                e.Graphics.FillRectangle(t, ClientRectangle);
            }
        }
    }

    // placa de metal com cantos cortados e rebites
    class PlacaMetal : Panel
    {
        public Color CorBorda = Tema.Borda;

        public PlacaMetal()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
            Padding = new Padding(6);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float s = DeviceDpi / 96f;
            var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (var forma = Tema.Chanfro(r, 8 * s))
            using (var fundo = new LinearGradientBrush(r, Color.FromArgb(242, 36, 40, 29), Color.FromArgb(242, 26, 29, 21), 90f))
            using (var borda = new Pen(CorBorda, 1.2f * s))
            {
                g.FillPath(fundo, forma);
                g.DrawPath(borda, forma);
            }
            using (var risco = new Pen(Color.FromArgb(70, Tema.Destaque), 2 * s))
                g.DrawLine(risco, 8 * s + 3 * s, 2 * s, 30 * s, 2 * s);
            float m = 6 * s, raio = 1.8f * s;
            using (var b = new SolidBrush(Tema.Rebite))
            {
                g.FillEllipse(b, r.Right - m - raio, r.Y + m - raio, raio * 2, raio * 2);
                g.FillEllipse(b, r.X + m - raio, r.Bottom - m - raio, raio * 2, raio * 2);
            }
        }
    }

    enum EstiloBotao { Cheio, Aba, Janela }

    // botão em forma de placa de blindagem, com texto em estêncil
    class BotaoMilitar : ControleDesenhado
    {
        Color cor = Tema.Oliva;
        bool ativo, emCima, apertado;
        public EstiloBotao Estilo = EstiloBotao.Cheio;

        public Color Cor { get => cor; set { cor = value; Invalidate(); } }
        public bool Ativo { get => ativo; set { ativo = value; Invalidate(); } }

        public BotaoMilitar()
        {
            Cursor = Cursors.Hand;
            TabStop = true;
            Font = Tema.Fonte(11, true);
            AccessibleRole = AccessibleRole.PushButton;
        }

        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
        protected override void OnMouseEnter(EventArgs e) { emCima = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { emCima = apertado = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { apertado = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { apertado = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) OnClick(EventArgs.Empty);
            base.OnKeyUp(e);
        }

        static Color Clarear(Color c, float f) =>
            Color.FromArgb(c.A, Math.Min(255, (int)(c.R + (255 - c.R) * f)), Math.Min(255, (int)(c.G + (255 - c.G) * f)), Math.Min(255, (int)(c.B + (255 - c.B) * f)));
        static Color Escurecer(Color c, float f) => Color.FromArgb(c.A, (int)(c.R * f), (int)(c.G * f), (int)(c.B * f));

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            float s = Escala;
            var r = new RectangleF(0.5f, 0.5f + (apertado ? 1 : 0), Width - 1.5f, Height - 1.5f - (apertado ? 1 : 0));
            Color texto = Tema.Texto;

            if (Estilo == EstiloBotao.Aba)
            {
                using (var forma = Tema.Chanfro(r, 7 * s))
                {
                    if (ativo || emCima)
                        using (var f = new SolidBrush(ativo ? Color.FromArgb(230, Tema.Oliva) : Color.FromArgb(120, Tema.Oliva)))
                            g.FillPath(f, forma);
                    using (var p = new Pen(ativo ? Tema.Destaque : Tema.Borda, 1.2f * s))
                        g.DrawPath(p, forma);
                }
                if (ativo)
                    using (var barra = new SolidBrush(Tema.Destaque))
                        g.FillRectangle(barra, r.X + 1, r.Y + 7 * s, 4 * s, r.Height - 14 * s);
                texto = ativo ? Tema.Texto : Tema.Sub;
                Escrever(g, r, texto, StringAlignment.Near, 18 * s);
                return;
            }

            Color baseCor = !Enabled ? Color.FromArgb(52, 56, 44) : emCima ? Clarear(cor, 0.12f) : cor;
            float corte = (Estilo == EstiloBotao.Janela ? 5 : 10) * s;
            using (var forma = Tema.Chanfro(r, corte))
            using (var fundo = new LinearGradientBrush(r, Clarear(baseCor, 0.08f), Escurecer(baseCor, 0.72f), 90f))
            using (var borda = new Pen(Enabled ? Clarear(baseCor, 0.35f) : Tema.Borda, 1.2f * s))
            {
                g.FillPath(fundo, forma);
                g.DrawPath(borda, forma);
            }
            if (Estilo == EstiloBotao.Cheio)
                Rebites(g, r, 7 * s, 1.8f * s);
            if (Focused && ShowFocusCues)
                using (var foco = new Pen(Tema.Texto, 1) { DashStyle = DashStyle.Dot })
                    g.DrawRectangle(foco, 4 * s, 4 * s, Width - 9 * s, Height - 9 * s);
            Escrever(g, r, Enabled ? texto : Tema.Sub, StringAlignment.Center, 0);
        }

        void Escrever(Graphics g, RectangleF r, Color cor, StringAlignment alinhamento, float recuo)
        {
            var area = new RectangleF(r.X + recuo, r.Y, r.Width - recuo, r.Height);
            using (var f = new StringFormat { Alignment = alinhamento, LineAlignment = StringAlignment.Center })
            using (var sombra = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
            using (var b = new SolidBrush(cor))
            {
                string t = Text.ToUpperInvariant();
                g.DrawString(t, Font, sombra, new RectangleF(area.X + 1, area.Y + 1, area.Width, area.Height), f);
                g.DrawString(t, Font, b, area, f);
            }
        }
    }

    // chave liga/desliga estilo painel de rádio militar
    class Alternador : ControleDesenhado
    {
        bool marcado;
        public event EventHandler CheckedChanged;

        public bool Checked
        {
            get => marcado;
            set { if (marcado == value) return; marcado = value; Invalidate(); CheckedChanged?.Invoke(this, EventArgs.Empty); }
        }

        public Alternador()
        {
            Cursor = Cursors.Hand;
            TabStop = true;
            Font = Tema.Fonte(11);
            AccessibleRole = AccessibleRole.CheckButton;
        }

        protected override void OnClick(EventArgs e) { Checked = !Checked; base.OnClick(e); }
        protected override void OnKeyUp(KeyEventArgs e) { if (e.KeyCode == Keys.Space) Checked = !Checked; base.OnKeyUp(e); }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            float s = Escala;
            var trilho = new RectangleF(1, (Height - 20 * s) / 2, 46 * s, 20 * s);
            using (var forma = Tema.Chanfro(trilho, 5 * s))
            using (var fundo = new SolidBrush(marcado ? Tema.Oliva : Color.FromArgb(40, 42, 34)))
            using (var borda = new Pen(marcado ? Tema.Verde : Tema.Borda, 1.2f * s))
            {
                g.FillPath(fundo, forma);
                g.DrawPath(borda, forma);
            }
            var alavanca = new RectangleF(marcado ? trilho.Right - 20 * s : trilho.X + 3 * s, trilho.Y + 3 * s, 17 * s, trilho.Height - 6 * s);
            using (var b = new LinearGradientBrush(alavanca, Color.FromArgb(200, 196, 170), Color.FromArgb(120, 118, 98), 90f))
                g.FillRectangle(b, alavanca);
            using (var luz = new SolidBrush(marcado ? Tema.Verde : Tema.Vermelho))
                g.FillEllipse(luz, marcado ? trilho.X + 8 * s : trilho.Right - 13 * s, trilho.Y + trilho.Height / 2 - 2.5f * s, 5 * s, 5 * s);

            var area = new RectangleF(trilho.Right + 10 * s, 0, Width - trilho.Right - 10 * s, Height);
            using (var f = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
            using (var b = new SolidBrush(Tema.Texto))
                g.DrawString(Text, Font, b, area, f);
            if (Focused && ShowFocusCues)
                using (var foco = new Pen(Tema.Destaque, 1) { DashStyle = DashStyle.Dot })
                    g.DrawRectangle(foco, 0, 0, Width - 1, Height - 1);
        }
    }

    // texto em estêncil com sombra, para status, contador e título
    class Estencil : ControleDesenhado
    {
        public StringAlignment Alinhamento = StringAlignment.Near;

        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
        protected override void OnForeColorChanged(EventArgs e) { base.OnForeColorChanged(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            float d = Math.Max(1, Font.Size / 12f) * Escala;
            using (var f = new StringFormat { Alignment = Alinhamento, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
            using (var sombra = new SolidBrush(Color.FromArgb(170, 0, 0, 0)))
            using (var b = new SolidBrush(ForeColor))
            {
                var r = new RectangleF(0, 0, Width, Height);
                g.DrawString(Text, Font, sombra, new RectangleF(d, d, Width, Height), f);
                g.DrawString(Text, Font, b, r, f);
            }
        }
    }

    // gráfico de cliques por segundo dos últimos 60 s, estilo tela de radar
    class Grafico : ControleDesenhado
    {
        public List<double> Dados = new List<double>();

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            float s = Escala;
            int w = Width, h = Height;
            var r = new RectangleF(0.5f, 0.5f, w - 1.5f, h - 1.5f);
            using (var forma = Tema.Chanfro(r, 8 * s))
            {
                using (var fundo = new SolidBrush(Color.FromArgb(242, 22, 26, 17)))
                    g.FillPath(fundo, forma);
                g.SetClip(forma);
                using (var grade = new Pen(Color.FromArgb(40, Tema.Verde)))
                {
                    for (float x = 0; x < w; x += 24 * s) g.DrawLine(grade, x, 0, x, h);
                    for (float y = 0; y < h; y += 24 * s) g.DrawLine(grade, 0, y, w, y);
                }

                double max = 10;
                foreach (var v in Dados) if (v > max) max = v;
                using (var fonte = Tema.Fonte(8.5f))
                using (var pincel = new SolidBrush(Tema.Sub))
                {
                    int topo = fonte.Height + 8;
                    g.DrawString("CLIQUES/S  ·  ÚLTIMOS 60 S", fonte, pincel, 10 * s, 4 * s);
                    string txtMax = $"MÁX {max:N0}";
                    g.DrawString(txtMax, fonte, pincel, w - g.MeasureString(txtMax, fonte).Width - 8 * s, 4 * s);

                    if (Dados.Count >= 2)
                    {
                        var pts = new PointF[Dados.Count];
                        for (int k = 0; k < Dados.Count; k++)
                            pts[k] = new PointF((float)(w - 2 - (Dados.Count - 1 - k) * (w - 4) / 59.0),
                                                (float)(h - 5 - Dados[k] / max * (h - topo - 10)));
                        using (var area = new GraphicsPath())
                        {
                            area.AddLines(pts);
                            area.AddLine(pts[pts.Length - 1].X, h, pts[0].X, h);
                            using (var preenche = new LinearGradientBrush(new RectangleF(0, topo, w, h - topo),
                                       Color.FromArgb(90, Tema.Destaque), Color.FromArgb(0, Tema.Destaque), 90f))
                                g.FillPath(preenche, area);
                        }
                        using (var linha = new Pen(Tema.Destaque, 2 * s))
                            g.DrawLines(linha, pts);
                    }
                }
                g.ResetClip();
                using (var borda = new Pen(Tema.Borda, 1.2f * s))
                    g.DrawPath(borda, forma);
            }
        }
    }
}
