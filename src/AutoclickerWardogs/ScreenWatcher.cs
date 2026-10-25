using System;
using System.Drawing;

namespace AutoclickerWardogs
{
    // Detecta quando a tela de facção some comparando uma grade de pixels em volta do alvo
    static class ScreenWatcher
    {
        const int Largura = 480, Altura = 260, Colunas = 16, Linhas = 10;

        public static Color[] Amostra(Point centro)
        {
            using (var bmp = new Bitmap(Largura, Altura))
            {
                using (var g = Graphics.FromImage(bmp))
                    g.CopyFromScreen(centro.X - Largura / 2, centro.Y - 150, 0, 0, bmp.Size);

                var cores = new Color[Colunas * Linhas];
                int i = 0;
                for (int y = 0; y < Linhas; y++)
                    for (int x = 0; x < Colunas; x++)
                        cores[i++] = bmp.GetPixel(x * (Largura - 1) / (Colunas - 1), y * (Altura - 1) / (Linhas - 1));
                return cores;
            }
        }

        public static double FracaoMudada(Color[] a, Color[] b)
        {
            int mudou = 0;
            for (int i = 0; i < a.Length; i++)
            {
                int d = Math.Abs(a[i].R - b[i].R) + Math.Abs(a[i].G - b[i].G) + Math.Abs(a[i].B - b[i].B);
                if (d > 45) mudou++;
            }
            return (double)mudou / a.Length;
        }
    }
}
