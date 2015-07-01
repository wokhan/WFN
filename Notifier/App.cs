using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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

        public App()
        {
            CommonHelper.OverrideSettingsFile("WFN.config");
        }

        public App(ReadOnlyCollection<string> argv)
        {
            CommonHelper.OverrideSettingsFile("WFN.config");

            NextInstance(argv);
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
                    string exclusionsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exclusions.set");
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
                        existing.LocalPortArray.Add(localport);
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
                            ProcessHelper.GetService(pid, threadid, path, protocol, localport, target, targetPort, out svc, out svcdsc, out unsure);
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
                    if (FirewallHelper.GetMatchingRules(path, protocol, target, targetPort, localport, unsure ? svc : svc.Take(1), true).Any())
                    {
                        return false;
                    }

                    FileVersionInfo fileinfo = null;
                    try
                    {
                        fileinfo = FileVersionInfo.GetVersionInfo(path);
                    }
                    catch { }

                    var conn = new CurrentConn
                    {
                        CurrentProd = app,
                        Editor = fileinfo != null ? fileinfo.CompanyName : String.Empty,
                        CurrentPath = path,
                        Protocol = int.Parse(protocol),
                        TargetPort = targetPort,
                        RuleName = String.Format(Common.Resources.RULE_NAME_FORMAT, unsure || String.IsNullOrEmpty(svcdsc.FirstOrDefault()) ? app : svcdsc.FirstOrDefault()),
                        Target = target,
                        LocalPort = localport
                    };

                    conn.LocalPortArray.Add(localport);

                    if (unsure)
                    {
                        conn.PossibleServices = svc;
                        conn.PossibleServicesDesc = svcdsc;
                    }
                    else
                    {
                        conn.CurrentService = svc.FirstOrDefault();
                        conn.CurrentServiceDesc = svcdsc.FirstOrDefault();
                    }

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
            try
            {
                var host = (await Dns.GetHostEntryAsync(conn.Target)).HostName;
                if (conn.Target != host)
                {
                    conn.ResolvedHost = host;
                }
            }
            catch { }
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

            if (window == null)
            {
                window = new NotificationWindow();
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                //this.Run(window);
            }

            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }
        }
    }
}
