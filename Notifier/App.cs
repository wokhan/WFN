using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Wokhan.WindowsFirewallNotifier.Common;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Notifier.Helpers;
using Wokhan.WindowsFirewallNotifier.Notifier.UI.Windows;

namespace Wokhan.WindowsFirewallNotifier.Notifier
{
    public class App : Application
    {
        NotificationWindow window;

        private ObservableCollection<CurrentConn> _conns = new ObservableCollection<CurrentConn>();
        public ObservableCollection<CurrentConn> Connections { get { return _conns; } }

        private string[] exclusions = null;

        public App() { }

        public App(ReadOnlyCollection<string> argv)
        {
            Dictionary<string, string> pars = ProcessHelper.ParseParameters(argv);
            int pid = int.Parse(pars["pid"]);
            string currentTarget = pars["ip"];
            string currentTargetPort = pars["port"];
            string currentProtocol = pars["protocol"];
            string currentLocalPort = pars["localport"];
            string currentPath = pars["path"];
            string threadid = pars["threadid"];
            pars = null;

            initExclusions();

            if (!AddItem(pid, threadid, currentPath, currentTarget, currentProtocol, currentTargetPort, currentLocalPort))
            {
                return;
            }

            LogHelper.Debug("Launching. Parameters: " + String.Join(" ", argv));

            window = new NotificationWindow();

            this.ShutdownMode = System.Windows.ShutdownMode.OnMainWindowClose;
            //this.Run(window);
        }

        /// <summary>
        /// 
        /// </summary>
        private void initExclusions()
        {
            try
            {
                if (!Settings.Default.UseBlockRules && exclusions == null)
                {
                    string exclusionsPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "exclusions.set");
                    if (File.Exists(exclusionsPath))
                    {
                        exclusions = File.ReadAllLines(exclusionsPath);
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to load the exceptions list.", e);
            }
        }

        public bool AddItem(int pid, string threadid, string path, string target, string protocol, string targetPort, string localport)
        {
            try
            {
                if (path != "System")
                {
                    path = CommonHelper.GetFriendlyPath(path);
                }

                var existing = this.Connections.FirstOrDefault(c => c.CurrentPath == path && c.Target == target && c.TargetPort == targetPort);// && (int.Parse(localport) >= 49152 || c.LocalPort == localport));
                if (existing != null)
                {
                    if (int.Parse(localport) < 49152)
                    {
                        existing.LocalPort += "," + localport;
                    }
                    existing.TentativesCounter++;
                }
                else
                {
                    string[] svc = new string[0];
                    string[] svcdsc = new string[0];
                    bool unsure = false;
                    string app = null;

                    if (path == "System")
                    {
                        app = "System";
                    }
                    else
                    {
                        try
                        {
                            if (File.Exists(path))
                            {
                                app = FileVersionInfo.GetVersionInfo(path).FileDescription;
                            }
                            else
                            {
                                app = path;
                            }
                        }
                        catch (Exception exc)
                        {
                            LogHelper.Error("Unable to check the file description.", exc);
                            app = path + " (not found)";
                        }

                        if (Settings.Default.EnableServiceDetection)
                        {
                            ProcessHelper.GetService(pid, threadid, protocol, targetPort, localport, out svc, out svcdsc, out unsure);
                        }
                    }

                    if (exclusions != null)
                    {
                        // WARNING: check for regressions
                        var exclusion = exclusions.FirstOrDefault(e => e.StartsWith(/*svc ??*/path, StringComparison.CurrentCultureIgnoreCase) || svc != null && svc.All(s => e.StartsWith(s, StringComparison.CurrentCultureIgnoreCase)));
                        if (exclusion != null)
                        {
                            string[] esplit = exclusion.Split(';');
                            if (esplit.Length == 1 ||
                                    ((esplit[1] == String.Empty || esplit[1] == localport) &&
                                     (esplit[2] == String.Empty || esplit[2] == target) &&
                                     (esplit[3] == String.Empty || esplit[3] == targetPort)))
                            {
                                return false;
                            }
                        }
                    }

                    // WARNING: check for regressions
                    if (FirewallHelper.CheckIfBlockingRuleMatches(path, protocol, targetPort, localport, svc, unsure))
                    {
                        return false;
                    }

                    var conn = new CurrentConn
                    {
                        CurrentProd = app,
                        CurrentPath = path,
                        CurrentService = !unsure ? svc.FirstOrDefault() : null,
                        PossibleServices = unsure ? svc : null,
                        CurrentServiceDesc = !unsure ? svcdsc.FirstOrDefault() : null,
                        PossibleServicesDesc = unsure ? svcdsc : null,
                        Protocol = int.Parse(protocol),
                        TargetPort = targetPort,
                        RuleName = String.Format(Wokhan.WindowsFirewallNotifier.Common.Resources.RULE_NAME_FORMAT, unsure || String.IsNullOrEmpty(svcdsc.FirstOrDefault()) ? app : svcdsc.FirstOrDefault()),
                        Target = target,
                        LocalPort = localport
                    };

                    resolveHostForConnection(conn);
                    //retrieveIcon(conn);
                    conn.Icon = ProcessHelper.GetIcon(conn.CurrentPath, true);

                    this.Connections.Add(conn);

                    return true;
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to add the connection to the pool.", e);
            }

            return false;
        }

        private async void retrieveIcon(CurrentConn conn)
        {
            var icon = await ProcessHelper.GetIconAsync(conn.CurrentPath, true);
            conn.Icon = icon;
        }

        private async void resolveHostForConnection(CurrentConn conn)
        {
            var host = (await Dns.GetHostEntryAsync(conn.Target)).HostName;
            if (conn.Target != host)
            {
                conn.ResolvedHost = host;
            }
        }


        internal void NextInstance(ReadOnlyCollection<string> argv)
        {
            Dictionary<string, string> pars = ProcessHelper.ParseParameters(argv);
            int pid = int.Parse(pars["pid"]);
            string currentTarget = pars["ip"];
            string currentTargetPort = pars["port"];
            string currentProtocol = pars["protocol"];
            string currentLocalPort = pars["localport"];
            string currentPath = pars["path"];
            string threadid = pars["threadid"];
            pars = null;

            initExclusions();

            if (!AddItem(pid, threadid, currentPath, currentTarget, currentProtocol, currentTargetPort, currentLocalPort))
            {
                return;
            }

            if (window.WindowState == System.Windows.WindowState.Minimized)
            {
                window.WindowState = System.Windows.WindowState.Normal;
            }
        }
    }
}
