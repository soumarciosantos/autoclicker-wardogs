using System;
using System.Threading;

namespace AutoclickerWardogs
{
    // Clica numa thread separada para a janela não travar
    class Clicker
    {
        public volatile int DelayMs = 5;
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
                while (rodando)
                {
                    Native.SetCursorPos(x, y);
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
