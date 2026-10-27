using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace AutoclickerWardogs
{
    // Cena de guerra animada: morteiros, explosões, helicóptero, caças, holofotes, traçantes e tanque.
    // A intensidade acompanha o humor do programa (parado, atacando, pausado, vitória).
    class Batalha : Control
    {
        class Particula
        {
            public float X, Y, Vx, Vy, Vida, Max, Tam;
            public int Tipo; // 0 fumaça, 1 faísca, 2 clarão, 3 sinalizador, 4 fogo
            public Color Cor;
        }

        class Morteiro { public float X, Y, Vx, Vy; }
        class Tracante { public float X, Y, Vx, Vy, Vida; }

        class Aeronave
        {
            public float X, Y, Vel, Fase;
            public bool Jato;
            public float ProximoTiro;
        }

        readonly Timer quadro = new Timer { Interval = 33 };
        readonly Random rnd = new Random();
        readonly List<Particula> particulas = new List<Particula>();
        readonly List<Morteiro> morteiros = new List<Morteiro>();
        readonly List<Tracante> tracantes = new List<Tracante>();
        readonly List<Aeronave> aeronaves = new List<Aeronave>();
        Bitmap cenario;
        Humor humor = Humor.Parado;
        float tempo, tremor, clarao, proximoMorteiro = 1, proximoHeli = 0.5f, proximoJato = 6, proximoTanque = 3, proximaRajada = 1, proximoSinal;
        PointF canoTanque, fendaBunker;

        public Humor Humor { get => humor; set { humor = value; } }

        public Batalha()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            quadro.Tick += (s, e) => Passo(0.033f);
        }

        protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); quadro.Enabled = Visible; }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { quadro.Dispose(); cenario?.Dispose(); }
            base.Dispose(disposing);
        }

        protected override void OnResize(EventArgs e) { base.OnResize(e); cenario?.Dispose(); cenario = null; }

        float S => Height / 110f;
        float Chao => Height * 0.8f;
        float Sorteio(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        // ---------- simulação ----------
        void Passo(float dt)
        {
            var form = FindForm();
            if (!Visible || form == null || form.WindowState == FormWindowState.Minimized) return;
            tempo += dt;
            bool ataque = humor == Humor.Clicando, calmo = humor == Humor.Pausado;

            // morteiros: raros parado, chuva quando atacando, nenhum em cessar-fogo
            proximoMorteiro -= dt;
            if (proximoMorteiro <= 0 && !calmo && humor != Humor.Feliz)
            {
                morteiros.Add(new Morteiro { X = Sorteio(0.05f, 0.95f) * Width, Y = -10, Vx = Sorteio(-30, 30) * S, Vy = Sorteio(60, 110) * S });
                proximoMorteiro = ataque ? Sorteio(0.08f, 0.28f) : Sorteio(1.2f, 3f);
            }

            // helicóptero
            proximoHeli -= dt;
            if (proximoHeli <= 0 && !aeronaves.Exists(a => !a.Jato))
            {
                bool direita = rnd.Next(2) == 0;
                aeronaves.Add(new Aeronave
                {
                    X = direita ? -50 * S : Width + 50 * S, Y = Sorteio(0.18f, 0.38f) * Height,
                    Vel = (direita ? 1 : -1) * (calmo ? 14 : ataque ? 42 : 26) * S, ProximoTiro = 0.6f
                });
                proximoHeli = Sorteio(3, 8);
            }

            // caças passando rápido
            proximoJato -= dt;
            if (proximoJato <= 0 && !calmo)
            {
                bool direita = rnd.Next(2) == 0;
                aeronaves.Add(new Aeronave { Jato = true, X = direita ? -40 * S : Width + 40 * S, Y = Sorteio(0.08f, 0.2f) * Height, Vel = (direita ? 1 : -1) * Sorteio(220, 300) * S });
                proximoJato = ataque ? Sorteio(3, 6) : Sorteio(9, 16);
            }

            // tanque disparando quando atacando
            proximoTanque -= dt;
            if (proximoTanque <= 0 && ataque)
            {
                Clarao(canoTanque.X + 3 * S, canoTanque.Y, 7 * S, Color.FromArgb(255, 230, 140));
                for (int i = 0; i < 4; i++) Fumaca(canoTanque.X + 4 * S, canoTanque.Y, 0.6f);
                tremor = Math.Max(tremor, 1.5f);
                proximoTanque = Sorteio(1.6f, 3f);
            }

            // traçantes do bunker
            proximaRajada -= dt;
            if (proximaRajada <= 0 && !calmo && humor != Humor.Feliz)
            {
                int tiros = ataque ? 6 : 3;
                float ang = Sorteio(-1.2f, -0.5f);
                for (int i = 0; i < tiros; i++)
                {
                    float a = ang + Sorteio(-0.05f, 0.05f), v = 260 * S;
                    tracantes.Add(new Tracante { X = fendaBunker.X - i * (float)Math.Cos(a) * 9 * S, Y = fendaBunker.Y - i * (float)Math.Sin(a) * 9 * S, Vx = (float)Math.Cos(a) * v, Vy = (float)Math.Sin(a) * v, Vida = 0.9f });
                }
                proximaRajada = ataque ? Sorteio(0.4f, 1f) : Sorteio(2.5f, 5f);
            }

            // sinalizadores de vitória
            if (humor == Humor.Feliz)
            {
                proximoSinal -= dt;
                if (proximoSinal <= 0)
                {
                    float x = Sorteio(0.15f, 0.85f) * Width, y = Sorteio(0.15f, 0.45f) * Height;
                    var cor = rnd.Next(3) == 0 ? Tema.Amarelo : rnd.Next(2) == 0 ? Tema.Verde : Color.FromArgb(240, 240, 220);
                    for (int i = 0; i < 26; i++)
                    {
                        double a = rnd.NextDouble() * Math.PI * 2; float v = Sorteio(20, 55) * S;
                        particulas.Add(new Particula { Tipo = 3, X = x, Y = y, Vx = (float)Math.Cos(a) * v, Vy = (float)Math.Sin(a) * v, Vida = 1.4f, Max = 1.4f, Tam = 2 * S, Cor = cor });
                    }
                    proximoSinal = Sorteio(0.35f, 0.8f);
                }
            }

            if (rnd.NextDouble() < 0.12)
            {
                float[] focos = { 0.24f, 0.47f, 0.9f };
                Fumaca(Width * focos[rnd.Next(3)], Chao - 6 * S, 0.8f);
            }

            // atualizar morteiros e explodir no chão
            for (int i = morteiros.Count - 1; i >= 0; i--)
            {
                var m = morteiros[i];
                m.Vy += 140 * S * dt; m.X += m.Vx * dt; m.Y += m.Vy * dt;
                float alvoY = Chao + Sorteio(-2, 6) * S;
                if (m.Y >= alvoY) { Explosao(m.X, alvoY, ataque ? 1.6f : 1.1f); morteiros.RemoveAt(i); }
            }

            // aeronaves
            for (int i = aeronaves.Count - 1; i >= 0; i--)
            {
                var a = aeronaves[i];
                a.X += a.Vel * dt; a.Fase += dt;
                if (!a.Jato)
                {
                    a.Y += (float)Math.Sin(a.Fase * 2) * 4 * S * dt;
                    a.ProximoTiro -= dt;
                    if (ataque && a.ProximoTiro <= 0)
                    {
                        // foguete do helicóptero vira morteiro inclinado
                        float dir = Math.Sign(a.Vel);
                        morteiros.Add(new Morteiro { X = a.X + dir * 10 * S, Y = a.Y + 6 * S, Vx = dir * 90 * S, Vy = 40 * S });
                        Clarao(a.X + dir * 12 * S, a.Y + 5 * S, 4 * S, Color.FromArgb(255, 220, 120));
                        a.ProximoTiro = Sorteio(0.5f, 1.2f);
                    }
                }
                else if (rnd.NextDouble() < 0.5)
                {
                    // rastro do caça
                    particulas.Add(new Particula { Tipo = 0, X = a.X - Math.Sign(a.Vel) * 12 * S, Y = a.Y + 1 * S, Vx = 0, Vy = -2 * S, Vida = 0.7f, Max = 0.7f, Tam = 2 * S, Cor = Color.FromArgb(160, 150, 130) });
                }
                if (a.X < -80 * S || a.X > Width + 80 * S) aeronaves.RemoveAt(i);
            }

            for (int i = tracantes.Count - 1; i >= 0; i--)
            {
                var t = tracantes[i];
                t.X += t.Vx * dt; t.Y += t.Vy * dt; t.Vida -= dt;
                if (t.Vida <= 0 || t.Y < -10) tracantes.RemoveAt(i);
            }

            for (int i = particulas.Count - 1; i >= 0; i--)
            {
                var p = particulas[i];
                p.Vida -= dt;
                if (p.Vida <= 0) { particulas.RemoveAt(i); continue; }
                p.X += p.Vx * dt; p.Y += p.Vy * dt;
                switch (p.Tipo)
                {
                    case 0: p.Tam += 9 * S * dt; p.Vx *= 0.98f; break;                // fumaça cresce e sobe
                    case 1: p.Vy += 160 * S * dt; break;                               // faísca cai
                    case 2: p.Tam += 60 * S * dt; break;                               // clarão expande
                    case 3: p.Vy += 25 * S * dt; p.Vx *= 0.97f; break;                 // sinalizador
                    case 4: p.Vy *= 0.94f; p.Tam *= 1 - 1.2f * dt; break;                // fogo sobe e encolhe
                }
            }
            if (particulas.Count > 500) particulas.RemoveRange(0, particulas.Count - 500);

            tremor = Math.Max(0, tremor - dt * 8);
            clarao = Math.Max(0, clarao - dt * 2.5f);
            Invalidate();
        }

        void Fumaca(float x, float y, float forca) =>
            particulas.Add(new Particula
            {
                Tipo = 0, X = x + Sorteio(-4, 4) * S, Y = y, Vx = Sorteio(-6, 6) * S, Vy = Sorteio(-16, -6) * S,
                Vida = Sorteio(1.2f, 2.4f) * forca + 0.4f, Max = 2.8f, Tam = Sorteio(3, 6) * S,
                Cor = Color.FromArgb(rnd.Next(70, 100), rnd.Next(70, 95), rnd.Next(60, 80))
            });

        void Clarao(float x, float y, float tam, Color cor) =>
            particulas.Add(new Particula { Tipo = 2, X = x, Y = y, Tam = tam, Vida = 0.18f, Max = 0.18f, Cor = cor });

        void Explosao(float x, float y, float forca)
        {
            Clarao(x, y, 8 * S * forca, Color.FromArgb(255, 240, 170));
            // bolas de fogo irregulares subindo
            for (int i = 0; i < 9 * forca; i++)
                particulas.Add(new Particula
                {
                    Tipo = 4, X = x + Sorteio(-6, 6) * S, Y = y - Sorteio(0, 5) * S, Vx = Sorteio(-16, 16) * S, Vy = Sorteio(-48, -18) * S,
                    Vida = Sorteio(0.35f, 0.7f), Max = 0.7f, Tam = Sorteio(3.5f, 7) * S * forca
                });
            for (int i = 0; i < 14 * forca; i++)
            {
                double a = -Math.PI * Sorteio(0.1f, 0.9f); float v = Sorteio(30, 90) * S * forca;
                particulas.Add(new Particula
                {
                    Tipo = 1, X = x, Y = y, Vx = (float)Math.Cos(a) * v, Vy = (float)Math.Sin(a) * v,
                    Vida = Sorteio(0.3f, 0.7f), Max = 0.7f, Tam = Sorteio(1, 2.2f) * S,
                    Cor = rnd.Next(2) == 0 ? Color.FromArgb(255, 200, 80) : Color.FromArgb(255, 120, 40)
                });
            }
            for (int i = 0; i < 6 * forca; i++) Fumaca(x, y - 3 * S, forca);
            clarao = Math.Min(1, clarao + 0.35f * forca);
            if (humor == Humor.Clicando) tremor = Math.Max(tremor, 2.5f * forca);
        }

        // ---------- desenho ----------
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            if (cenario == null) cenario = DesenharCenario();
            float s = S;
            var estado = g.Save();
            if (tremor > 0) g.TranslateTransform(Sorteio(-tremor, tremor), Sorteio(-tremor, tremor));
            g.DrawImageUnscaled(cenario, 0, 0);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // clarão das explosões iluminando o céu
            if (clarao > 0)
                using (var luz = new SolidBrush(Color.FromArgb((int)(90 * clarao), 255, 150, 60)))
                    g.FillRectangle(luz, 0, 0, Width, Chao);

            Holofotes(g, s);
            Incendios(g, s);

            foreach (var a in aeronaves) if (a.Jato) Jato(g, a, s); else Helicoptero(g, a, s);

            using (var rastro = new Pen(Color.FromArgb(160, 255, 220, 140), 1.4f * s))
            using (var cabeca = new SolidBrush(Color.FromArgb(255, 255, 240, 200)))
                foreach (var m in morteiros)
                {
                    float len = 0.09f;
                    g.DrawLine(rastro, m.X - m.Vx * len, m.Y - m.Vy * len, m.X, m.Y);
                    g.FillEllipse(cabeca, m.X - 1.6f * s, m.Y - 1.6f * s, 3.2f * s, 3.2f * s);
                }

            using (var tr = new Pen(Color.FromArgb(230, 255, 210, 90), 1.3f * s))
                foreach (var t in tracantes)
                    g.DrawLine(tr, t.X, t.Y, t.X - t.Vx * 0.025f, t.Y - t.Vy * 0.025f);

            foreach (var p in particulas)
            {
                float f = Math.Max(0, p.Vida / p.Max);
                switch (p.Tipo)
                {
                    case 0:
                        using (var b = new SolidBrush(Color.FromArgb((int)(150 * f), p.Cor)))
                            g.FillEllipse(b, p.X - p.Tam, p.Y - p.Tam, p.Tam * 2, p.Tam * 2);
                        break;
                    case 1:
                    case 3:
                        using (var b = new SolidBrush(Color.FromArgb((int)(255 * f), p.Cor)))
                            g.FillEllipse(b, p.X - p.Tam / 2, p.Y - p.Tam / 2, p.Tam, p.Tam);
                        break;
                    case 2:
                        using (var b = new SolidBrush(Color.FromArgb((int)(230 * f), p.Cor)))
                            g.FillEllipse(b, p.X - p.Tam, p.Y - p.Tam, p.Tam * 2, p.Tam * 2);
                        break;
                    case 4:
                        // de amarelo quente a vermelho escuro conforme apaga
                        var cor = Color.FromArgb((int)(235 * f), 255, (int)(90 + 150 * f), (int)(30 * f));
                        using (var b = new SolidBrush(cor))
                            g.FillEllipse(b, p.X - p.Tam, p.Y - p.Tam * 0.8f, p.Tam * 2, p.Tam * 1.6f);
                        break;
                }
            }
            g.Restore(estado);

            // moldura
            using (var borda = new Pen(Tema.Borda, 1.2f * (DeviceDpi / 96f)))
                g.DrawRectangle(borda, 0, 0, Width - 1, Height - 1);
            using (var fonte = Tema.Fonte(8.5f, true))
            using (var b = new SolidBrush(Color.FromArgb(200, Tema.Destaque)))
            {
                string rotulo = humor == Humor.Clicando ? "● FRONT DE BATALHA · BOMBARDEIO" :
                                humor == Humor.Pausado ? "● CESSAR-FOGO" :
                                humor == Humor.Feliz ? "● VITÓRIA!" : "● FRONT DE BATALHA";
                g.DrawString(rotulo, fonte, b, 6 * s, 4 * s);
            }
        }

        // focos de fogo fixos no campo, com chama tremulando e brilho no chão
        void Incendios(Graphics g, float s)
        {
            foreach (var px in new[] { 0.24f, 0.47f, 0.9f })
            {
                float x = Width * px, y = Chao + 2 * s;
                float tremula = 0.75f + 0.25f * (float)Math.Sin(tempo * 9 + px * 20) + Sorteio(-0.08f, 0.08f);
                using (var brilho = new SolidBrush(Color.FromArgb((int)(55 * tremula), 255, 120, 30)))
                    g.FillEllipse(brilho, x - 16 * s, y - 9 * s, 32 * s, 14 * s);
                using (var chama = new SolidBrush(Color.FromArgb(220, 255, 110, 20)))
                    g.FillPolygon(chama, new[] { new PointF(x - 5 * s, y), new PointF(x - 1 * s, y - 11 * s * tremula), new PointF(x + 1.5f * s, y - 5 * s), new PointF(x + 4 * s, y - 8 * s * tremula), new PointF(x + 6 * s, y) });
                using (var miolo = new SolidBrush(Color.FromArgb(230, 255, 220, 120)))
                    g.FillPolygon(miolo, new[] { new PointF(x - 2.5f * s, y), new PointF(x, y - 6 * s * tremula), new PointF(x + 2.5f * s, y) });
            }
        }

        void Holofotes(Graphics g, float s)
        {
            for (int i = 0; i < 2; i++)
            {
                float baseX = Width * (i == 0 ? 0.3f : 0.62f), baseY = Chao;
                double ang = -Math.PI / 2 + Math.Sin(tempo * (0.35 + i * 0.12) + i * 2) * 0.7;
                float comp = Height * 1.1f, abertura = 0.09f;
                var pts = new[]
                {
                    new PointF(baseX, baseY),
                    new PointF(baseX + (float)Math.Cos(ang - abertura) * comp, baseY + (float)Math.Sin(ang - abertura) * comp),
                    new PointF(baseX + (float)Math.Cos(ang + abertura) * comp, baseY + (float)Math.Sin(ang + abertura) * comp)
                };
                using (var caminho = new GraphicsPath())
                {
                    caminho.AddPolygon(pts);
                    using (var b = new PathGradientBrush(caminho) { CenterPoint = pts[0], CenterColor = Color.FromArgb(70, 230, 230, 200), SurroundColors = new[] { Color.FromArgb(0, 230, 230, 200) } })
                        g.FillPath(b, caminho);
                }
            }
        }

        void Helicoptero(Graphics g, Aeronave a, float s)
        {
            var estado = g.Save();
            g.TranslateTransform(a.X, a.Y);
            g.ScaleTransform(Math.Sign(a.Vel) * s, s);
            g.RotateTransform(6); // nariz levemente baixo, como em voo
            using (var corpo = new SolidBrush(Color.FromArgb(14, 16, 11)))
            {
                g.FillEllipse(corpo, -12, -5, 22, 11);
                g.FillPolygon(corpo, new[] { new PointF(-10, -2), new PointF(-32, -1), new PointF(-32, 1.5f), new PointF(-10, 3) });
                g.FillPolygon(corpo, new[] { new PointF(-30, -1), new PointF(-35, -7), new PointF(-32, 1) });
                using (var p = new Pen(corpo.Color, 1.2f))
                {
                    g.DrawLine(p, -8, 8, 8, 8);
                    g.DrawLine(p, -4, 5, -5, 8); g.DrawLine(p, 4, 5, 5, 8);
                    g.DrawLine(p, -1, -5, -1, -8);
                }
            }
            using (var vidro = new SolidBrush(Color.FromArgb(90, 110, 130, 110)))
                g.FillEllipse(vidro, 2, -3, 7, 5);
            // hélice principal girando
            float giro = (float)Math.Abs(Math.Sin(a.Fase * 40));
            using (var helice = new Pen(Color.FromArgb(150, 30, 30, 26), 1.3f))
            {
                g.DrawLine(helice, -1 - 20 * giro, -8, -1 + 20 * giro, -8);
                g.DrawLine(helice, -1 - 20 * (1 - giro), -8.5f, -1 + 20 * (1 - giro), -8.5f);
            }
            using (var cauda = new Pen(Color.FromArgb(120, 30, 30, 26), 1f))
                g.DrawEllipse(cauda, -36, -6, 4 * giro + 1, 6);
            // luz de navegação piscando
            if ((int)(a.Fase * 2.5) % 2 == 0)
                using (var luz = new SolidBrush(Color.FromArgb(255, 255, 60, 40)))
                    g.FillEllipse(luz, -34, -8, 2.4f, 2.4f);
            g.Restore(estado);
        }

        void Jato(Graphics g, Aeronave a, float s)
        {
            var estado = g.Save();
            g.TranslateTransform(a.X, a.Y);
            g.ScaleTransform(Math.Sign(a.Vel) * s, s);
            using (var corpo = new SolidBrush(Color.FromArgb(16, 18, 13)))
                g.FillPolygon(corpo, new[]
                {
                    new PointF(12, 0), new PointF(4, -1.5f), new PointF(-2, -1.5f), new PointF(-6, -7), new PointF(-9, -7),
                    new PointF(-7, -1.5f), new PointF(-12, -1.5f), new PointF(-14, -5), new PointF(-16, -5), new PointF(-15, 1.5f),
                    new PointF(-7, 1.5f), new PointF(-9, 5), new PointF(-6, 5), new PointF(-1, 1.5f), new PointF(4, 1.5f)
                });
            using (var fogo = new SolidBrush(Color.FromArgb(200, 255, 150, 50)))
                g.FillEllipse(fogo, -20, -1.2f, 5 + (float)rnd.NextDouble() * 2, 2.4f);
            g.Restore(estado);
        }

        // céu, morros, bunker, arame e tanque: desenhados uma vez e reaproveitados
        Bitmap DesenharCenario()
        {
            int w = Math.Max(1, Width), h = Math.Max(1, Height);
            float s = S, chao = Chao;
            var bmp = new Bitmap(w, h);
            var r = new Random(3);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var ceu = new LinearGradientBrush(new Rectangle(0, 0, w, (int)chao + 1), Color.FromArgb(14, 16, 12), Color.FromArgb(104, 66, 30), 90f))
                    g.FillRectangle(ceu, 0, 0, w, chao + 1);
                using (var estrela = new SolidBrush(Color.FromArgb(120, 230, 230, 210)))
                    for (int i = 0; i < 40; i++)
                        g.FillRectangle(estrela, (float)r.NextDouble() * w, (float)r.NextDouble() * chao * 0.6f, 1, 1);

                Morros(g, r, w, chao, chao * 0.62f, 0.12f * h, Color.FromArgb(40, 38, 24));
                Morros(g, r, w, chao, chao * 0.78f, 0.08f * h, Color.FromArgb(26, 27, 17));

                using (var terra = new LinearGradientBrush(new RectangleF(0, chao, w, h - chao + 1), Color.FromArgb(28, 30, 20), Color.FromArgb(14, 15, 10), 90f))
                    g.FillRectangle(terra, 0, chao, w, h - chao + 1);

                var escuro = Color.FromArgb(10, 11, 8);
                using (var b = new SolidBrush(escuro))
                {
                    // bunker com fenda de tiro
                    float bx = w * 0.07f, bw = 38 * s, bh = 14 * s;
                    g.FillPolygon(b, new[] { new PointF(bx, chao + 2 * s), new PointF(bx + 5 * s, chao - bh), new PointF(bx + bw - 5 * s, chao - bh), new PointF(bx + bw, chao + 2 * s) });
                    fendaBunker = new PointF(bx + bw * 0.62f, chao - bh * 0.55f);
                    using (var fenda = new SolidBrush(Color.FromArgb(255, 200, 90)))
                        g.FillRectangle(fenda, bx + bw * 0.35f, chao - bh * 0.6f, bw * 0.3f, 2 * s);

                    // sacos de areia
                    using (var areia = new SolidBrush(Color.FromArgb(44, 42, 30)))
                        for (int i = 0; i < 7; i++)
                            g.FillEllipse(areia, bx + bw + 4 * s + i * 7 * s, chao - 4 * s, 8 * s, 5 * s);

                    // tanque
                    float tx = w * 0.78f, ty = chao;
                    g.FillPolygon(b, new[] { new PointF(tx, ty), new PointF(tx + 4 * s, ty - 6 * s), new PointF(tx + 40 * s, ty - 6 * s), new PointF(tx + 44 * s, ty) });
                    g.FillPolygon(b, new[] { new PointF(tx + 12 * s, ty - 6 * s), new PointF(tx + 15 * s, ty - 12 * s), new PointF(tx + 30 * s, ty - 12 * s), new PointF(tx + 32 * s, ty - 6 * s) });
                    canoTanque = new PointF(tx - 2 * s, ty - 10 * s);
                    using (var cano = new Pen(escuro, 2.2f * s))
                        g.DrawLine(cano, tx + 16 * s, ty - 9.5f * s, canoTanque.X, canoTanque.Y);
                    using (var esteira = new Pen(Color.FromArgb(40, 42, 30), 1f * s))
                        for (int i = 0; i < 6; i++) g.DrawEllipse(esteira, tx + 4 * s + i * 6.5f * s, ty - 4 * s, 4 * s, 4 * s);
                }

                // arame farpado
                using (var arame = new Pen(Color.FromArgb(70, 72, 60), 0.8f * s))
                {
                    float ay = chao + 7 * s;
                    for (float x = w * 0.32f; x < w * 0.72f; x += 10 * s)
                    {
                        g.DrawLine(arame, x, ay, x + 10 * s, ay);
                        g.DrawLine(arame, x + 3 * s, ay - 2 * s, x + 5 * s, ay + 2 * s);
                        g.DrawLine(arame, x + 5 * s, ay - 2 * s, x + 3 * s, ay + 2 * s);
                    }
                    for (float x = w * 0.32f; x < w * 0.72f; x += 40 * s)
                        g.DrawLine(arame, x, ay - 5 * s, x, ay + 4 * s);
                }
            }
            return bmp;
        }

        static void Morros(Graphics g, Random r, int w, float chao, float baseY, float amp, Color cor)
        {
            var pts = new List<PointF> { new PointF(0, chao + 1) };
            double f1 = r.NextDouble() * 6, f2 = r.NextDouble() * 6;
            for (int x = 0; x <= w; x += 6)
                pts.Add(new PointF(x, baseY - (float)((Math.Sin(x * 0.012 + f1) * 0.6 + Math.Sin(x * 0.031 + f2) * 0.4) * amp)));
            pts.Add(new PointF(w, chao + 1));
            using (var b = new SolidBrush(cor))
                g.FillPolygon(b, pts.ToArray());
        }
    }
}
