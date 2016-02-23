using System;
using System.Collections.Generic;
using System.ComponentModel;
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
                    argv = new string[] { "-pid", Process.GetCurrentProcess().Id.ToString(), "-threadid", "0", "-ip", "127.0.0.1", "-port", "0", "-protocol", "0", "-localport", "0", "-path", "DEMO MODE", "-impersonated", "1" };// + new Random().Next().ToString() };
                }

                Dictionary<string, string> pars = ProcessHelper.ParseParameters(argv);
                int pid = int.Parse(pars["pid"]);

                // Check if impersonation has already taken place (to avoid repeated relaunched and infinite loops)
                string impersonated = "0";
                pars.TryGetValue("impersonated", out impersonated);
                if (impersonated != "1")
                {
                    EnsureUserSession(pid, argv);
                }

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
            if (WindowsIdentity.GetCurrent().IsSystem)
            {
                uint targetSessionID = uint.MaxValue;

                IntPtr userToken = IntPtr.Zero;

                // Retrieves the target process Session ID
                try
                {
                    using (Process srcPr = Process.GetProcessById(pid))
                    {
                        targetSessionID = (uint)srcPr.SessionId;
                    }
                }
                catch (ArgumentException)
                {
                    LogHelper.Warning("Unable to retrieve the target process SessionID. Process may have already exited.");
                }

                // If the target Session ID is still unknown or if it belongs to SYSTEM, the currently active session is retrieved.
                if (targetSessionID == uint.MaxValue || targetSessionID == 0 || targetSessionID == 4)
                {
                    targetSessionID = CommonHelper.WTSGetActiveConsoleSessionId();
                    if (targetSessionID == 0xFFFFFFFF)
                    {
                        throw new Exception("No active session found. Aborting.");
                    }
                }

                // If the active Session ID is still a SYSTEM one, cannot continue.
                if (targetSessionID == 0 || targetSessionID == 4)
                {
                    throw new Exception("WFN can not start in the SYSTEM session.");
                }

                // Because the target Session ID is found, impersonation can take place.
                if (!CommonHelper.WTSQueryUserToken(targetSessionID, ref userToken))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to retrieve the current user token.");
                }

                string argstr = String.Join(" ", argv.Select(a => a.Contains(" ") ? "\"" + a + "\"" : a).ToArray()) + " -impersonated 1";
                LogHelper.Debug("Impersonating. Parameters: " + argstr);

                Impersonation.LaunchProcessAsUser(Process.GetCurrentProcess().MainModule.FileName, argstr, userToken);

                Environment.Exit(0);
            }
        }
    }
}
