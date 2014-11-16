using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using WindowsFirewallNotifier.Managers;

namespace WindowsFirewallNotifier
{

    static class Program
    {
        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        static void Main(string[] argv)
        {
            try
            {
                SingletonManager manager = new SingletonManager();

                if (argv.Length == 0 || argv[1].Contains("$"))
                {
                    argv = new string[] { "-pid", new Random().Next().ToString(), "-threadid", "0", "-ip", "127.0.0.1", "-port", "0", "-protocol", "0", "-localport", "0", "-path", "DEMO MODE _ " + new Random().Next().ToString() };
                }

                manager.Run(argv);
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to initialize WFN", e);

                Environment.Exit(0);
            }
        }
    }
}
