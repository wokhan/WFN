using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
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

        public static Dictionary<int, ProcessHelper.ServiceInfoResult> SERVICES = ProcessHelper.GetAllServicesByPidWMI();

        /// <summary>
        /// Main entrypoint of the application.
        /// </summary>
        [STAThread]
        static void Main(string[] argv)
        {
            try
            {
                LogHelper.Info("Checking access rights...");
                if (!IsUserAdministrator())
                {
                    LogHelper.Error("User must have admin rights to access to run Notifier.", null);
                    MessageBox.Show($"User must have admin rights to run Notifier\nNotifier will exit now!", "Not enough priviledge", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(1);
                }

                App app = new App();
                app.Run();
            }
            catch (Exception e)
            {
                LogHelper.Error(e.Message, e);
                Environment.Exit(1);
            }
            Environment.Exit(0);
        }

        public App() : base()
        {
            this.ShutdownMode = ShutdownMode.OnMainWindowClose;
            CommonHelper.OverrideSettingsFile("WFN.config");

            LogHelper.Debug("Initializing exclusions...");
            initExclusions();

            LogHelper.Debug("Init notification window...");
            window = new NotificationWindow
            {
                WindowState = WindowState.Normal
            };

            LogHelper.Debug("Start security log polling task...");
            _ = EventLogPollingTaskAsync(1_000);

            LogHelper.Debug("Start update services task...");
            _ = UpdateServiceInfoTaskAsync(30_000);

        }

        internal async Task UpdateServiceInfoTaskAsync(int waitMillis)
        {
            try
            {
                DateTime timeStamp = DateTime.Now;
                while (true)
                {
                    await Task.Delay(waitMillis).ConfigureAwait(false);
                    Dictionary<int, ProcessHelper.ServiceInfoResult> dict = ProcessHelper.GetAllServicesByPidWMI();
                    SERVICES = dict;
                    Console.WriteLine($"-> {DateTime.Now} Update service info");
                }
            } 
            catch (Exception e)
            {
                LogHelper.Error($"UpdateServiceInfoTask exception", e);
                MessageBox.Show($"UpdateServiceInfoTask exception:\n{e.Message}\nNotifier will exit.", "Update service info exception", MessageBoxButton.OK, MessageBoxImage.Error);
                window.Close();
            }
        }

        internal async Task EventLogPollingTaskAsync(int waitMillis)
        {
            try
            {
                DateTime lastLogEntryTimeStamp = DateTime.Now;
                while (true)
                {
                    using (EventLog securityLog = new EventLog("security"))
                    {
                        List<EventLogEntry> newEntryList = new List<EventLogEntry>();
                        int entryIndex = securityLog.Entries.Count - 1;
                        for (int i = entryIndex; i >= 0; i--)
                        {
                            EventLogEntry entry = securityLog.Entries[i];
                            bool isNewEntry = entry.TimeWritten > lastLogEntryTimeStamp;
                            if (isNewEntry)
                            {
                                if (FirewallHelper.isEventInstanceIdAccepted(entry.InstanceId))
                                {
                                    newEntryList.Insert(0, entry);
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        lastLogEntryTimeStamp = securityLog.Entries[entryIndex].TimeWritten;

                        foreach (EventLogEntry entry in newEntryList)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                // dispatch to ui thread
                                HandleEventLogNotification(entry);
                            });
                        }
                    }
                    await Task.Delay(waitMillis).ConfigureAwait(false);
                }
            }

            catch (SecurityException se)
            {
                LogHelper.Error("Security event log polling task exception: " + se.Message, se);
                MessageBox.Show($"Notifier cannot access security event log:\n{se.Message}\nNotifier needs to be started with admin rights.\nNotifier will exit.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                window.Close();
            }
            catch (Exception e)
            {
                LogHelper.Error("Security event log polling task exception: " + e.Message, e);
                MessageBox.Show($"Security log polling exception:\n{e.Message}\nNotifier will exit.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                window.Close();
            }
        }

        internal static bool IsUserAdministrator()
        {
            bool isAdmin;
            try
            {
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (UnauthorizedAccessException ex)
            {
                isAdmin = false;
            }
            catch (Exception ex)
            {
                isAdmin = false;
            }
            return isAdmin;
        }

        enum Direction
        {
            In, Out
        }

        private void HandleEventLogNotification(EventLogEntry entry)
        {

            try
            {
                int pid = int.Parse(GetReplacementString(entry, 0));
                int threadId = 0;
                Direction direction = GetReplacementString(entry, 2) == @"%%14593" ? Direction.Out : Direction.In;
                int protocol = int.Parse(GetReplacementString(entry, 7));
                string targetIp = GetReplacementString(entry, 5);
                int targetPort = int.Parse(GetReplacementString(entry, 6));
                string sourceIp = GetReplacementString(entry, 3);
                int sourcePort = int.Parse(GetReplacementString(entry, 4));
                string friendlyPath = GetReplacementString(entry, 1) == "-" ? "System" : FileHelper.GetFriendlyPath(GetReplacementString(entry, 1));
                string fileName = System.IO.Path.GetFileName(friendlyPath);

                // try to get the servicename from pid (works only if service is running)
                string serviceName = SERVICES.ContainsKey(pid) ? SERVICES[pid].Name : "-";

                LogHelper.Debug($"Adding {direction}-going connection for '{fileName}', service: {serviceName} ...");
                if (!AddItem(pid, threadId, friendlyPath, targetIp, protocol, targetPort, sourcePort))
                {
                    //This connection is blocked by a specific rule. No action necessary.
                    LogHelper.Debug($"{direction} connection for '{fileName}' is blocked by a rule - ignored.");
                    return;
                }

                if (window.WindowState == WindowState.Minimized)
                {
                    window.ShowActivityTrayIcon($"Notifier blocked connections - click tray icon to show");  // max 64 chars
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("Error in HandleEventLogNotification", e);
            }
        }

        private static string GetReplacementString(EventLogEntry entry, int i)
        {
            // check out of bounds
            if (i < entry.ReplacementStrings.Length)
            {
                return entry.ReplacementStrings[i];
            }
            else
            {
                return "";
            }
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
        internal bool AddItem(int pid, int threadid, string path, string target, int protocol, int targetPort, int localPort)
        {
            try
            {
                string fileName = System.IO.Path.GetFileName(path);
                if (path != "System")
                {
                    path = FileHelper.GetFriendlyPath(path);
                }

                //FIXME: Do a proper path compare...? CASE!
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
                    string description = null;

                    if (path == "System")
                    {
                        description = "System";
                    }
                    else
                    {
                        try
                        {
                            if (File.Exists(path))
                            {
                                description = FileVersionInfo.GetVersionInfo(path).FileDescription;
                                if(String.IsNullOrWhiteSpace(description))
                                {
                                    description = path.Substring(path.LastIndexOf('\\') + 1);
                                }
                            }
                            else
                            {
                                description = path;
                            }
                        }
                        catch (Exception exc)
                        {
                            LogHelper.Error("Unable to check the file description.", exc);
                            description = path + " (not found)";
                        }

                        if (Settings.Default.EnableServiceDetection)
                        {
                            ProcessHelper.ServiceInfoResult svcInfo = null;
                            if (SERVICES.TryGetValue(pid, out svcInfo))
                            {
                                svc = new string[] { svcInfo.Name };
                                svcdsc = new string[] { svcInfo.DisplayName };
                                unsure = false;
                                LogHelper.Debug($"Service detected for '{fileName}': '{svc[0]}'");
                            } else
                            {
                                //ProcessHelper.GetService(pid, threadid, path, protocol, localPort, target, targetPort, out svc, out svcdsc, out unsure);
                                LogHelper.Debug($"No service detected for '{fileName}'");
                            }
                        }
                    }

                    // Check whether this connection has been excluded - exclusion means ignore i.e do not notify
                    if (exclusions != null)
                    {
                        // WARNING: check for regressions
                        LogHelper.Debug("Checking exclusions...");
                        var exclusion = exclusions.FirstOrDefault(e => e.StartsWith(/*svc ??*/path, StringComparison.InvariantCulture) || svc != null && svc.All(s => e.StartsWith(s, StringComparison.InvariantCulture)));
                        if (exclusion != null)
                        {
                            string[] esplit = exclusion.Split(';');
                            if (esplit.Length == 1 ||
                                    (String.IsNullOrEmpty(esplit[1]) || esplit[1] == localPort.ToString()) &&
                                    (String.IsNullOrEmpty(esplit[2]) || esplit[2] == target) &&
                                    (String.IsNullOrEmpty(esplit[3]) || esplit[3] == targetPort.ToString())
                               )
                            {
                                LogHelper.Info($"Connection is excluded: {exclusion}");
                                return false;
                            }
                        }
                    }

                    // Check whether this connection is blocked by a rule.
                    var blockingRules = FirewallHelper.GetMatchingRules(path, ProcessHelper.getAppPkgId(pid), protocol, target, targetPort.ToString(), localPort.ToString(), unsure ? svc : svc.Take(1), ProcessHelper.getLocalUserOwner(pid), blockOnly:true, outgoingOnly:true);
                    if (blockingRules.Any())
                    {
                        LogHelper.Info("Connection matches a block-rule!");

                        StringBuilder sb = new StringBuilder();
                        sb.Append("Blocked by: ");
                        foreach (FirewallHelper.Rule s in blockingRules)
                        {
                            sb.Append(s.Name + ": " + s.ApplicationName + ", " + s.Description + ", " + s.ActionStr + ", " + s.ServiceName + ", " + s.Enabled);
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
                        Description = description,
                        CurrentAppPkgId = ProcessHelper.getAppPkgId(pid),
                        CurrentLocalUserOwner = ProcessHelper.getLocalUserOwner(pid),
                        ProductName = fileinfo != null ? fileinfo.ProductName : String.Empty,
                        Company = fileinfo != null ? fileinfo.CompanyName : String.Empty,
                        CurrentPath = path,
                        Protocol = protocol,
                        TargetPort = targetPort.ToString(),
                        RuleName = String.Format(Common.Properties.Resources.RULE_NAME_FORMAT, unsure || String.IsNullOrEmpty(svcdsc.FirstOrDefault()) ? description : svcdsc.FirstOrDefault()),
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

                    ResolveHostForConnection(conn);
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

        private static async void ResolveHostForConnection(CurrentConn conn)
        {
            try
            {
                conn.ResolvedHost = "...";
                var host = (await DnsResolver.ResolveIpAddress(conn.Target).ConfigureAwait(true)).HostEntry.HostName; 
                //var host = (await Dns.GetHostEntryAsync(conn.Target)).HostName;
                if (conn.Target != host)
                {
                    conn.ResolvedHost = host;
                }
            }
            catch (Exception e) 
            {
                LogHelper.Warning($"Cannot resolve host name for {conn.Target} - Exception: {e.Message}");
            }
        }
    }
}
