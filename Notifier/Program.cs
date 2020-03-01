using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;

using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Notifier.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Notifier
{
    internal static class Program
    {
        private const string ARG_IMPERSONATED = "impersonated";

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Main entrypoint of the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] argv)
        {
            LogHelper.Debug("Starting Notifier: " + Environment.CommandLine);
            try
            {
                if (argv.Length == 0 || argv[1].Contains("$"))
                {
                    argv = new string[] { "-pid", Process.GetCurrentProcess().Id.ToString(), "-threadid", "0", "-ip", "127.0.0.1", "-port", "0", "-protocol", "0", "-localport", "0", "-path", "DEMO MODE", "-impersonated", "1" };// + new Random().Next().ToString() };
                }

                var parsedParameters = ProcessHelper.ParseParameters(argv);
                var pid = int.Parse(parsedParameters["pid"]);

                // Check if impersonation has already taken place (to avoid repeated relaunch and infinite loops)
                if (parsedParameters.TryGetValue(ARG_IMPERSONATED, out var impersonated) && impersonated != "1")
                {
                    EnsureUserSession(pid, argv);
                }

                SingleInstanceApp<Dictionary<string, string>>.RunOrJoin<App>(parsedParameters);
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to initialize WFN", e);
                Environment.Exit(1);
            }
            Environment.Exit(0);
        }

        private static void EnsureUserSession(int pid, string[] argv)
        {
            if (WindowsIdentity.GetCurrent().IsSystem)
            {
                var targetSessionID = uint.MaxValue;

                IntPtr userToken = IntPtr.Zero;

                // Retrieves the target process Session ID
                try
                {
                    using (var srcPr = Process.GetProcessById(pid))
                    {
                        targetSessionID = (uint)srcPr.SessionId;
                    }
                }
                catch (ArgumentException)
                {
                    LogHelper.Warning("Unable to retrieve the target process SessionID. Process may have already exited.");
                    targetSessionID = uint.MaxValue;
                }

                // If the target Session ID is still unknown or if it belongs to SYSTEM, or the session doesn't work, the currently active session is retrieved.
                if (targetSessionID == uint.MaxValue || targetSessionID == 0 || (!CommonHelper.WTSQueryUserToken(targetSessionID, ref userToken)))
                {
                    targetSessionID = CommonHelper.WTSGetActiveConsoleSessionId();
                    if (targetSessionID == 0xFFFFFFFF)
                    {
                        throw new Exception("No active session found. Aborting.");
                    }

                    // If the active Session ID is still a SYSTEM one, cannot continue.
                    if (targetSessionID == 0)
                    {
                        throw new Exception("WFN can not start in the SYSTEM session.");
                    }

                    // Because the target Session ID is found, impersonation can take place.
                    if (!CommonHelper.WTSQueryUserToken(targetSessionID, ref userToken))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to retrieve the current user token.");
                    }
                }

                try
                {
                    var argstr = String.Join(" ", argv.Select(a => $"\"{a}\"")) + $" -{ARG_IMPERSONATED} 1";
                    LogHelper.Debug("Impersonating. Parameters: " + argstr);

                    Impersonation.LaunchProcessAsUser(Process.GetCurrentProcess().MainModule.FileName, argstr, userToken);
                }
                finally
                {
                    CloseHandle(userToken);
                }

                Environment.Exit(0);
            }
        }
    }
}
