using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace AutoclickerWardogs
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Native.SetProcessDPIAware();
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            bool atualizado = args.Contains("--atualizado");

            using (var unico = new Mutex(false, "AutoclickerWardogs_unico"))
            {
                bool liberado;
                // depois de atualizar, espera a versão antiga terminar de fechar
                try { liberado = unico.WaitOne(atualizado ? 10000 : 0); }
                catch (AbandonedMutexException) { liberado = true; }
                if (!liberado)
                {
                    MessageBox.Show("O Autoclicker Wardogs já está aberto.", "Autoclicker Wardogs", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Updater.LimparVersaoAntiga();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm(atualizado));
                unico.ReleaseMutex();
            }
        }
    }
}
