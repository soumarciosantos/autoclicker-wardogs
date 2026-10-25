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
        // de quanto em quanto tempo o mouse sai do card e volta (0 = nunca) e quanto ele desce
        public volatile int SaidaMs = 1000;
        public volatile int SaidaPx = 150;
        const int EsperaForaMs = 60;
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
            // Jogos com cursor próprio leem o movimento bruto do mouse (Raw Input) e ignoram SetCursorPos.
            // Por isso o mouse é movido com deslocamentos relativos, que chegam ao jogo como um mouse físico.
            int deslocado = 0; // onde o mouse está em relação ao alvo, em px
            var desdeSaida = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                Native.SetCursorPos(x, y);
                bool direita = false;
                while (rodando)
                {
                    if (SaidaMs > 0 && desdeSaida.ElapsedMilliseconds >= SaidaMs)
                    {
                        SairEVoltar();
                        desdeSaida.Restart();
                        if (!rodando) break;
                    }

                    // alterna um passo para a esquerda e um para a direita, para o jogo perceber o mouse em cima do card
                    int t = Tremor;
                    direita = !direita;
                    int destino = direita ? t : -t;
                    if (destino != deslocado)
                    {
                        Native.mouse_event(Native.MOUSEEVENTF_MOVE, destino - deslocado, 0, 0, UIntPtr.Zero);
                        deslocado = destino;
                    }
                    Native.mouse_event(Native.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                    Native.mouse_event(Native.MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                    Interlocked.Increment(ref total);
                    Thread.Sleep(DelayMs);
                }
            }
            finally
            {
                // devolve o mouse ao ponto de partida
                if (deslocado != 0) Native.mouse_event(Native.MOUSEEVENTF_MOVE, -deslocado, 0, 0, UIntPtr.Zero);
                Native.timeEndPeriod(1);
            }
        }

        // Tira o mouse do card (para baixo, longe dos outros times), espera e volta, sem clicar.
        // As esperas dão tempo de o jogo desenhar alguns quadros e perceber que o mouse saiu e entrou.
        void SairEVoltar()
        {
            int d = SaidaPx;
            if (d <= 0) return;
            Native.mouse_event(Native.MOUSEEVENTF_MOVE, 0, d, 0, UIntPtr.Zero);
            Thread.Sleep(EsperaForaMs);
            Native.mouse_event(Native.MOUSEEVENTF_MOVE, 0, -d, 0, UIntPtr.Zero);
            Thread.Sleep(EsperaForaMs);
        }
    }
}
