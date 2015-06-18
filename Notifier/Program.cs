using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Notifier.Managers;

namespace Wokhan.WindowsFirewallNotifier.Notifier
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
                if (argv.Length == 0 || argv[1].Contains("$"))
                {
                    argv = new string[] { "-pid", Process.GetCurrentProcess().Id.ToString(), "-threadid", "0", "-ip", "127.0.0.1", "-port", "0", "-protocol", "0", "-localport", "0", "-path", "DEMO MODE" };// + new Random().Next().ToString() };
                }

                Dictionary<string, string> pars = ProcessHelper.ParseParameters(argv);
                int pid = int.Parse(pars["pid"]);

                EnsureUserSession(pid, argv);

                new SingletonManager().Run(argv);
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to initialize WFN", e);

                Environment.Exit(0);
            }
        }

        private static void EnsureUserSession(int pid, string[] argv)
        {
            Process currentProc = Process.GetCurrentProcess();
            uint SessID = (uint)currentProc.SessionId;

            if (WindowsIdentity.GetCurrent().IsSystem)
            {
                IntPtr userToken = IntPtr.Zero;
                try
                {
                    using (Process srcPr = Process.GetProcessById(pid))
                    {
                        SessID = (uint)srcPr.SessionId;
                    }
                }
                catch { }

                if (SessID == uint.MaxValue || SessID == 4 || SessID == 0)
                {
                    // Retrieves the currently active session if the process was not running
                    SessID = (uint)CommonHelper.WTSGetActiveConsoleSessionId();
                    if (SessID == 0xFFFFFFFF)
                    {
                        Exception e = new Exception("No active session found. Aborting.");
                        LogHelper.Error("FATAL ERROR", e);
                        throw e;
                    }
                    /*else
                    {
                        int errCode = Marshal.GetLastWin32Error();
                        if (errCode != 0)
                        {
                            throw new Exception("Unable to retrieve the active session ID. ErrCode = " + errCode);
                        }
                    }*/
                }

                if (SessID != 0 && SessID != currentProc.SessionId)
                {
                    if (!CommonHelper.WTSQueryUserToken(SessID, ref userToken))
                    {
                        Exception e = new Exception("Unable to retrieve the current user token. ErrCode = " + Marshal.GetLastWin32Error());
                        LogHelper.Error("FATAL ERROR", e);
                        throw e;
                    }

                    string argstr = String.Join(" ", argv.Select(a => a.Contains(" ") ? "\"" + a + "\"" : a).ToArray());
                    LogHelper.Debug("Impersonating. Parameters: " + argstr);
                    Impersonation.LaunchProcessAsUser(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Notifier.exe"), argstr, userToken);
                }
                else
                {
                    Exception e = new Exception("WFN can not start in the SYSTEM session.");
                    LogHelper.Error("FATAL ERROR", e);
                    throw e;
                }

                Environment.Exit(0);
            }
        }
    }
}
