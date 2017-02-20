using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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

        public App(ReadOnlyCollection<string> argv) : this()
        {
            NextInstance(argv);
        }

        /// <summary>
        /// 
        /// </summary>
        private void initExclusions()
        {
            try
            {
                if (!Settings.Default.UseBlockRules && exclusions == null) //@wokhan: WHY NOT~Settings.Default.UseBlockRules ??
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

        /// <summary>
        /// Add item to internal query list (asking user whether to allow this connection request), if there is no block rule available.
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="threadid"></param>
        /// <param name="path"></param>
        /// <param name="target"></param>
        /// <param name="protocol"></param>
        /// <param name="targetPort"></param>
        /// <param name="localPort"></param>
        /// <returns>false if item is blocked and was thus not added to internal query list</returns>
        public bool AddItem(int pid, int threadid, string path, string target, int protocol, int targetPort, int localPort)
        {
            try
            {
                if (path != "System")
                {
                    path = FileHelper.GetFriendlyPath(path);
                }

                var existing = this.Connections.FirstOrDefault(c => c.CurrentPath == path && c.Target == target && c.TargetPort == targetPort.ToString() && (localPort >= IPHelper.GetMaxUserPort() || c.LocalPort == localPort.ToString()) && c.Protocol == protocol);
                if (existing != null)
                {
                    LogHelper.Debug("Matches an already existing connection request.");
                    if (!existing.LocalPortArray.Contains(localPort))
                    {
                        existing.LocalPortArray.Add(localPort);
                        existing.LocalPortArray.Sort(); //Note: Unfortunately, C# doesn't have a simple List that automatically sorts... :(
                        existing.LocalPort = IPHelper.MergePorts(existing.LocalPortArray);
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
                                if(String.IsNullOrEmpty(app))
                                {
                                    app = path.Substring(path.LastIndexOf('\\') + 1);
                                }
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
                            ProcessHelper.GetService(pid, threadid, path, protocol, localPort, target, targetPort, out svc, out svcdsc, out unsure);
                        }
                    }

                    // Check whether this connection has been excluded.
                    if (exclusions != null)
                    {
                        // WARNING: check for regressions
                        LogHelper.Debug("Checking exclusions...");
                        var exclusion = exclusions.FirstOrDefault(e => e.StartsWith(/*svc ??*/path, StringComparison.CurrentCultureIgnoreCase) || svc != null && svc.All(s => e.StartsWith(s, StringComparison.CurrentCultureIgnoreCase)));
                        if (exclusion != null)
                        {
                            string[] esplit = exclusion.Split(';');
                            if (esplit.Length == 1 ||
                                    ((esplit[1] == String.Empty || esplit[1] == localPort.ToString()) &&
                                     (esplit[2] == String.Empty || esplit[2] == target) &&
                                     (esplit[3] == String.Empty || esplit[3] == targetPort.ToString())))
                            {
                                LogHelper.Info("Connection is excluded!");
                                return false;
                            }
                        }
                    }

                    // Check whether this connection is blocked by a rule.
                    var blockingRules = FirewallHelper.GetMatchingRules(path, protocol, target, targetPort.ToString(), localPort.ToString(), unsure ? svc : svc.Take(1), true);
                    if (blockingRules.Any())
                    {
                        LogHelper.Info("Connection matches a block-rule!");

                        StringBuilder sb = new StringBuilder();
                        sb.Append("Blocked by: ");
                        foreach (FirewallHelper.Rule s in blockingRules)
                        {
                            sb.Append(s.Name + ": " + s.ApplicationName + ", " + s.Description + ", " + s.ActionStr + ", " + s.serviceName + ", " + s.Enabled);
                        }
                        LogHelper.Debug("pid: " + Process.GetCurrentProcess().Id + " GetMatchingRules: " + path + ", " + protocol + ", " + target + ", " + targetPort + ", " + localPort + ", " + String.Join(",", svc));

                        return false;
                    }

                    FileVersionInfo fileinfo = null;
                    try
                    {
                        fileinfo = FileVersionInfo.GetVersionInfo(path);
                    }
                    catch (FileNotFoundException)
                    { }

                    var conn = new CurrentConn
                    {
                        CurrentProd = app,
                        Editor = fileinfo != null ? fileinfo.CompanyName : String.Empty,
                        CurrentPath = path,
                        Protocol = protocol,
                        TargetPort = targetPort.ToString(),
                        RuleName = String.Format(Common.Resources.RULE_NAME_FORMAT, unsure || String.IsNullOrEmpty(svcdsc.FirstOrDefault()) ? app : svcdsc.FirstOrDefault()),
                        Target = target,
                        LocalPort = localPort.ToString()
                    };

                    conn.LocalPortArray.Add(localPort);

                    if (unsure)
                    {
                        //LogHelper.Debug("Adding services (unsure): " + String.Join(",", svc));
                        conn.PossibleServices = svc;
                        conn.PossibleServicesDesc = svcdsc;
                    }
                    else
                    {
                        //LogHelper.Debug("Adding services: " + svc.FirstOrDefault());
                        conn.CurrentService = svc.FirstOrDefault();
                        conn.CurrentServiceDesc = svcdsc.FirstOrDefault();
                    }

                    resolveHostForConnection(conn);
                    //retrieveIcon(conn);
                    conn.Icon = IconHelper.GetIcon(conn.CurrentPath, true);

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
            var icon = await IconHelper.GetIconAsync(conn.CurrentPath, true);
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
            try
            {
                Dictionary<string, string> pars = ProcessHelper.ParseParameters(argv);
                int pid = int.Parse(pars["pid"]);
                int threadid = int.Parse(pars["threadid"]);
                string currentTarget = pars["ip"];
                int currentTargetPort = int.Parse(pars["port"]);
                int currentProtocol = int.Parse(pars["protocol"]);
                int currentLocalPort = int.Parse(pars["localport"]);
                string currentPath = pars["path"];
                pars = null;

                LogHelper.Debug("Initializing exclusions...");
                initExclusions();

                LogHelper.Debug("Adding item...");
                if (!AddItem(pid, threadid, currentPath, currentTarget, currentProtocol, currentTargetPort, currentLocalPort))
                {
                    //This connection is blocked. No action necessary.
                    LogHelper.Info("Connection is blocked.");
                    if (window == null)
                    {
                        LogHelper.Debug("No notification window loaded; shutting down...");
                        this.Shutdown();
                    }
                    return;
                }

                LogHelper.Debug("Displaying notification window...");
                if (window == null)
                {
                    LogHelper.Debug("No notification window loaded; creating a new one...");
                    window = new NotificationWindow();
                    this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                    //this.Run(window);
                }

                if (window.WindowState == WindowState.Minimized)
                {
                    window.WindowState = WindowState.Normal;
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("Error in NextInstance", e);
            }

        }
    }
}
