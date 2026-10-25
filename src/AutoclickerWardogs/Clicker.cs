using System;
using System.Threading;

namespace AutoclickerWardogs
{
    // Clica numa thread separada para a janela não travar
    class Clicker
    {
        public volatile int DelayMs = 5;
        // pixels que o mouse anda para cada lado entre os cliques (0 = parado)
        public volatile int Tremor = 1;
        int x, y;
        long total;
        volatile bool rodando;

        public long Total => Interlocked.Read(ref total);
        public bool Rodando => rodando;

        public void Zerar() => Interlocked.Exchange(ref total, 0);
        public void Parar() => rodando = false;

        public void Iniciar(int px, int py)
        {
            if (rodando) return;
            x = px; y = py; rodando = true;
            new Thread(Loop) { IsBackground = true, Name = "Clicker" }.Start();
        }

        void Loop()
        {
            Native.timeBeginPeriod(1); // Sleep com precisão de 1 ms
            try
            {
                bool direita = false;
                while (rodando)
                {
                    // alterna um passo para a esquerda e um para a direita, para o jogo perceber o mouse em cima do card
                    int t = Tremor;
                    direita = !direita;
                    Native.SetCursorPos(x + (direita ? t : -t), y);
                    Native.mouse_event(Native.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                    Native.mouse_event(Native.MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                    Interlocked.Increment(ref total);
                    Thread.Sleep(DelayMs);
                }
            }
            finally
            {
                Native.timeEndPeriod(1);
            }
        }
    }
}
