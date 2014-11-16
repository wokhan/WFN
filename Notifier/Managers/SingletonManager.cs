using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

namespace WindowsFirewallNotifier.Managers
{
    class SingletonManager : WindowsFormsApplicationBase, IDisposable
    {
        private MainForm window;

        public SingletonManager()
        {
            IsSingleInstance = true;
            EnableVisualStyles = false;
        }

        protected override bool OnStartup(StartupEventArgs e)
        {
            window = new MainForm(int.Parse(e.CommandLine[1]), e.CommandLine[3], e.CommandLine[13], e.CommandLine[5], e.CommandLine[9], e.CommandLine[7], e.CommandLine[11]);
            System.Windows.Forms.Application.Run(window);

            return false;
        }

        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs e)
        {
            base.OnStartupNextInstance(e);

            window.AddItem(int.Parse(e.CommandLine[1]), e.CommandLine[3], e.CommandLine[13], e.CommandLine[5], e.CommandLine[9], e.CommandLine[7], e.CommandLine[11], true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static Dictionary<string, string> parseParameters(ReadOnlyCollection<string> args)
        {
            Dictionary<string, string> ret = null;
            try
            {
                ret = new Dictionary<string, string>(args.Count / 2);
                for (int i = 0; i < args.Count; i += 2)
                {
                    ret.Add(args[i], args[i + 1]);
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to parse the parameters: argv = " + String.Join(" ", args), e);
            }

            return ret;
        }


        /// <summary>
        /// 
        /// </summary>
        private static void showNotification(ReadOnlyCollection<string> argv)
        {
            MainForm mainForm;
            Process currentProc = Process.GetCurrentProcess();

            Dictionary<string, string> pars = parseParameters(argv);
            int pid = int.Parse(pars["-pid"]);

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
                    Impersonation.LaunchProcessAsUser(Application.ExecutablePath, argstr, userToken);
                }
                else
                {
                    Exception e = new Exception("WFN can not start in the SYSTEM session.");
                    LogHelper.Error("FATAL ERROR", e);
                    throw e;
                }

                Environment.Exit(0);
            }

            string currentTarget = pars["-ip"];
            string currentTargetPort = pars["-port"];
            string currentProtocol = pars["-protocol"];
            string currentLocalPort = pars["-localport"];
            string currentPath = pars["-path"];
            string threadid = pars["-threadid"];
            pars = null;

            LogHelper.Debug("Launching. Parameters: " + String.Join(" ", argv));
            mainForm = new MainForm(pid, threadid, currentPath, currentTarget, currentProtocol, currentTargetPort, currentLocalPort);
            if (!mainForm.IsDisposed)
            {
                Application.Run(mainForm);
            }
        }



        public void Dispose()
        {
            if (window != null)
                window.Dispose();
        }
    }
}
