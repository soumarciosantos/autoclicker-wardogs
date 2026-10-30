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

    // Página de conteúdo com fundo claro.
    class Pagina : Panel
    {
        public Pagina()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
            BackColor = Tema.Fundo;
        }

        protected override void OnPaintBackground(PaintEventArgs e) => e.Graphics.Clear(Tema.Fundo);
    }

    // Superfície leve com cantos suaves.
    class PlacaMetal : Panel
    {
        public Color CorBorda = Tema.Borda;
        public bool Hero;
        public Color Acento = Color.Empty;

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
            using (var forma = Tema.Arredondado(r, 14 * s))
            using (var fundo = Hero
                ? (Brush)new LinearGradientBrush(r, Color.FromArgb(38, 56, 45), Color.FromArgb(21, 34, 32), 0f)
                : new SolidBrush(Tema.Painel))
            using (var borda = new Pen(CorBorda, 1f * s))
            {
                g.FillPath(fundo, forma);
                g.DrawPath(borda, forma);
                if (Acento != Color.Empty)
                {
                    g.SetClip(forma);
                    using (var faixa = new SolidBrush(Acento)) g.FillRectangle(faixa, 0, 0, 4 * s, Height);
                    g.ResetClip();
                }
            }
        }
    }

    enum EstiloBotao { Cheio, Aba, Janela }

    // Botão sólido ou item de navegação.
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
            Font = Tema.Fonte(10.5f, true);
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
                using (var forma = Tema.Arredondado(r, 9 * s))
                {
                    if (ativo || emCima)
                        using (var f = new SolidBrush(ativo ? Color.FromArgb(48, 63, 51) : Color.FromArgb(34, 46, 40)))
                            g.FillPath(f, forma);
                    if (ativo)
                        using (var p = new Pen(Tema.Borda, 1f * s)) g.DrawPath(p, forma);
                }
                if (ativo)
                    using (var barra = new SolidBrush(Tema.Destaque))
                        g.FillRectangle(barra, r.X + 1, r.Y + 9 * s, 3 * s, r.Height - 18 * s);
                texto = ativo ? Tema.Destaque : Tema.Sub;
                Escrever(g, r, texto, StringAlignment.Near, 17 * s);
                return;
            }

            Color baseCor = !Enabled ? Color.FromArgb(49, 60, 55) : emCima ? Clarear(cor, 0.08f) : cor;
            using (var forma = Tema.Arredondado(r, (Estilo == EstiloBotao.Janela ? 8 : 10) * s))
            using (var fundo = new SolidBrush(baseCor))
            using (var borda = new Pen(Enabled ? baseCor : Tema.Borda, 1f * s))
            {
                g.FillPath(fundo, forma);
                g.DrawPath(borda, forma);
            }
            if (Focused && ShowFocusCues)
                using (var foco = new Pen(Tema.Destaque, 1) { DashStyle = DashStyle.Dot })
                    g.DrawRectangle(foco, 4 * s, 4 * s, Width - 9 * s, Height - 9 * s);
            Color textColor = !Enabled ? Tema.Sub :
                cor == Tema.Destaque || cor == Tema.Verde || cor == Tema.Amarelo ? Tema.Fundo : Tema.Texto;
            Escrever(g, r, textColor, StringAlignment.Center, 0);
        }

        void Escrever(Graphics g, RectangleF r, Color cor, StringAlignment alinhamento, float recuo)
        {
            var area = new RectangleF(r.X + recuo, r.Y, r.Width - recuo, r.Height);
            using (var f = new StringFormat { Alignment = alinhamento, LineAlignment = StringAlignment.Center })
            using (var b = new SolidBrush(cor))
            {
                g.DrawString(Text, Font, b, area, f);
            }
        }
    }

    // Chave acessível com estado visível.
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
            var trilho = new RectangleF(1, (Height - 22 * s) / 2, 42 * s, 22 * s);
            using (var forma = Tema.Arredondado(trilho, 11 * s))
            using (var fundo = new SolidBrush(marcado ? Tema.Destaque : Color.FromArgb(75, 89, 78)))
            {
                g.FillPath(fundo, forma);
            }
            var alavanca = new RectangleF(marcado ? trilho.Right - 19 * s : trilho.X + 3 * s, trilho.Y + 3 * s, 16 * s, 16 * s);
            using (var b = new SolidBrush(Tema.Fundo)) g.FillEllipse(b, alavanca);

            var area = new RectangleF(trilho.Right + 10 * s, 0, Width - trilho.Right - 10 * s, Height);
            using (var f = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
            using (var b = new SolidBrush(Tema.Texto))
                g.DrawString(Text, Font, b, area, f);
            if (Focused && ShowFocusCues)
                using (var foco = new Pen(Tema.Destaque, 1) { DashStyle = DashStyle.Dot })
                    g.DrawRectangle(foco, 0, 0, Width - 1, Height - 1);
        }
    }

    // Texto desenhado sem sombra, com aparo para janelas menores.
    class Estencil : ControleDesenhado
    {
        public StringAlignment Alinhamento = StringAlignment.Near;

        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
        protected override void OnForeColorChanged(EventArgs e) { base.OnForeColorChanged(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            using (var f = new StringFormat { Alignment = Alinhamento, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter })
            using (var b = new SolidBrush(ForeColor))
            {
                var r = new RectangleF(0, 0, Width, Height);
                g.DrawString(Text, Font, b, r, f);
            }
        }
    }

    // Gráfico simples de cliques por segundo dos últimos 60 s.
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
            using (var forma = Tema.Arredondado(r, 12 * s))
            {
                using (var fundo = new SolidBrush(Tema.Painel))
                    g.FillPath(fundo, forma);
                g.SetClip(forma);
                using (var grade = new Pen(Tema.Grade))
                {
                    for (float y = 34 * s; y < h; y += 24 * s) g.DrawLine(grade, 12 * s, y, w - 12 * s, y);
                }

                double max = 10;
                foreach (var v in Dados) if (v > max) max = v;
                using (var fonte = Tema.Fonte(8.5f))
                using (var pincel = new SolidBrush(Tema.Sub))
                {
                    int topo = fonte.Height + 8;
                    g.DrawString("ATIVIDADE · ÚLTIMOS 60 SEGUNDOS", fonte, pincel, 14 * s, 10 * s);
                    string txtMax = $"PICO {max:N0}";
                    g.DrawString(txtMax, fonte, pincel, w - g.MeasureString(txtMax, fonte).Width - 8 * s, 4 * s);

                    if (Dados.Count < 2)
                    {
                        using (var grande = Tema.Fonte(10))
                        using (var meio = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        using (var apagado = new SolidBrush(Tema.Sub))
                            g.DrawString("A atividade aparecerá aqui ao iniciar uma sessão", grande, apagado, new RectangleF(0, topo, w, h - topo), meio);
                    }
                    else
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
                using (var borda = new Pen(Tema.Borda, 1f * s))
                    g.DrawPath(borda, forma);
            }
        }
    }

    // Varredura abstrata Wardogs: só desenha, não interage com o clicker.
    class PulsoVisual : ControleDesenhado
    {
        readonly Timer animacao = new Timer { Interval = 70 };
        float fase;
        Humor humor = Humor.Parado;

        public Humor Humor { get => humor; set { humor = value; Invalidate(); } }

        public PulsoVisual()
        {
            animacao.Tick += (s, e) => { fase += humor == Humor.Clicando ? 0.095f : 0.035f; Invalidate(); };
            animacao.Start();
            AccessibleName = "Animação decorativa de atividade";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) animacao.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float s = Escala;
            float cx = Width / 2f, cy = Height / 2f;
            var cor = humor == Humor.Clicando ? Tema.Verde : humor == Humor.Pausado ? Tema.Amarelo : Tema.Destaque;
            float raio = Math.Min(Width, Height) * 0.42f;
            using (var linha = new Pen(Color.FromArgb(55, Tema.Sub), s))
            {
                for (int i = 1; i <= 3; i++)
                {
                    float r = raio * i / 3f;
                    g.DrawEllipse(linha, cx - r, cy - r, 2 * r, 2 * r);
                }
                g.DrawLine(linha, cx - raio, cy, cx + raio, cy);
                g.DrawLine(linha, cx, cy - raio, cx, cy + raio);
            }
            float graus = fase * 180f / (float)Math.PI;
            using (var brilho = new SolidBrush(Color.FromArgb(42, cor)))
                g.FillPie(brilho, cx - raio, cy - raio, raio * 2, raio * 2, graus - 38, 38);
            float theta = fase;
            float x = cx + (float)Math.Cos(theta) * raio;
            float y = cy + (float)Math.Sin(theta) * raio;
            using (var sweep = new Pen(Color.FromArgb(180, cor), 2f * s)) g.DrawLine(sweep, cx, cy, x, y);
            using (var ponto = new SolidBrush(cor)) g.FillEllipse(ponto, x - 4 * s, y - 4 * s, 8 * s, 8 * s);
            using (var ponto = new SolidBrush(Color.FromArgb(120, Tema.Verde)))
            {
                g.FillEllipse(ponto, cx - raio * .62f, cy - raio * .42f, 5 * s, 5 * s);
                g.FillEllipse(ponto, cx + raio * .56f, cy + raio * .28f, 5 * s, 5 * s);
            }
        }
    }
}
