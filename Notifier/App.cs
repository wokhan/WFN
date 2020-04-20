using log4net;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows;
using Wokhan.WindowsFirewallNotifier.Common.Config;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Common.Net.DNS;
using Wokhan.WindowsFirewallNotifier.Common.Net.IP;
using Wokhan.WindowsFirewallNotifier.Common.Net.WFP;
using Wokhan.WindowsFirewallNotifier.Common.Net.WFP.Rules;
using Wokhan.WindowsFirewallNotifier.Notifier.Helpers;
using Wokhan.WindowsFirewallNotifier.Notifier.UI.Windows;
using WinForms = System.Windows.Forms;

namespace Wokhan.WindowsFirewallNotifier.Notifier
{
    /// <summary>
    /// Notifier2 main program
    /// </summary>
    public sealed class App : Application, IDisposable
    {
        // Use log4net directly in case LogHelper throws exceptions during startup
        private readonly static ILog LOGGER = LogManager.GetLogger(typeof(App));

        private static App APP_INSTANCE;
        private static NotificationWindow notifierWindow;
        private static ActivityWindow activityWindow;

        private ObservableCollection<CurrentConn> _conns = new ObservableCollection<CurrentConn>();
        public ObservableCollection<CurrentConn> Connections { get { return _conns; } }

        private string[] exclusions = null;

        private readonly AsyncTaskRunner asyncTaskRunner;


        /// <summary>
        /// Main entrypoint of the application.
        /// </summary>
        [STAThread]
        static void Main(string[] argv)
        {
            try
            {
                LOGGER.Info("Checking access rights...");
                if (!IsUserAdministrator())
                {
                    LOGGER.Error("User must have admin rights to access to run Notifier.", null);
                    MessageBox.Show($"User must have admin rights to run Notifier\nNotifier will exit now!", "Security check", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(1);
                }
                string[] args = Environment.GetCommandLineArgs();
                // Ensures that notifier is only started once.
                using var mtex = new Mutex(true, "MTX_NotificationWindowInstance", out bool instanceCountOne);
                if (instanceCountOne)
                {
                    // TODO: maybe not required - remove?
                    WinForms::Application.EnableVisualStyles();
                    WinForms::Application.SetCompatibleTextRenderingDefault(false);

                    APP_INSTANCE = new App();
                    APP_INSTANCE.Run();
                    mtex.ReleaseMutex();
                }
                else
                {
                    LOGGER.Warn("A notififer instance is already running - showing it.");
                    MessageBox.Show("A notifier instance is already running");
                    APP_INSTANCE.ShowNotifierWindow();  // FIXME: show it - seems not to work as it should
                }
            }
            catch (Exception e)
            {
                // use log4net directly in case LogHelper throws an exception itself during startup
                LOGGER.Error(e.Message, e);  
                Environment.Exit(1);
            }
            Environment.Exit(0);
        }

        public App() : base()
        {
            this.ShutdownMode = ShutdownMode.OnMainWindowClose;

            LOGGER.Info("Initializing exclusions...");
            initExclusions();

            LOGGER.Info("Init notification window...");
            notifierWindow = new NotificationWindow
            {
                WindowState = WindowState.Normal
            };
            MainWindow = notifierWindow;
            activityWindow = ActivityWindow.Init(notifierWindow);
            if (Settings.Default.ActivityWindow_Shown)
            {
                activityWindow.Show();
            }

            asyncTaskRunner = new AsyncTaskRunner(this);
            asyncTaskRunner.StartTasks();
        }

        public static ActivityWindow GetActivityWindow()
        {
            return activityWindow;
        }

        public void ShowNotifierWindow()
        {
            notifierWindow.RestoreWindowState();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            asyncTaskRunner.CancelTasks();
            if (notifierWindow != null)
            {
                notifierWindow.Close();
            }
            base.OnExit(e);
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
            catch
            {
                isAdmin = false;
            }
            return isAdmin;
        }

        enum Direction
        {
            In, Out
        }

        internal void HandleEventLogNotification(EventLogEntry entry)
        {

            try
            {
                int pid = int.Parse(GetReplacementString(entry, 0));
                Direction direction = GetReplacementString(entry, 2) == @"%%14593" ? Direction.Out : Direction.In;
                int protocol = int.Parse(GetReplacementString(entry, 7));
                string targetIp = GetReplacementString(entry, 5);
                int targetPort = int.Parse(GetReplacementString(entry, 6));
                string sourceIp = GetReplacementString(entry, 3);
                int sourcePort = int.Parse(GetReplacementString(entry, 4));
                string friendlyPath = GetReplacementString(entry, 1) == "-" ? "System" : Common.IO.Files.PathResolver.GetFriendlyPath(GetReplacementString(entry, 1));
                string fileName = System.IO.Path.GetFileName(friendlyPath);

                // try to get the servicename from pid (works only if service is running)
                string serviceName = AsyncTaskRunner.GetServicName(pid);

                LogHelper.Info($"Handle {direction.ToString().ToUpper(CultureInfo.InvariantCulture)}-going connection for '{fileName}', service: {serviceName} ...");
                if (!AddItem(pid, friendlyPath, targetIp, protocol: protocol, targetPort: targetPort, localPort: sourcePort))
                {
                    //This connection is blocked by a specific rule. No action necessary.
                    LogHelper.Info($"{direction}-going connection for '{fileName}' is blocked by a rule - ignored.");
                    return;
                }

                //if (notifierWindow.WindowState == WindowState.Minimized)
                //{
                //    notifierWindow.ShowActivityTrayIcon($"Notifier blocked connections - click tray icon to show");  // max 64 chars!
                //}
            }
            catch (Exception e)
            {
                LogHelper.Error("HandleEventLogNotification exception", e);
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
            // FIXME: Remove - only use global block rules.
            try
            {
                if (!Settings.Default.UseBlockRules && exclusions is null) //@wokhan: WHY NOT~Settings.Default.UseBlockRules ??
                {
                    string exclusionsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exclusions.set");
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
        /// <param name="path"></param>
        /// <param name="target"></param>
        /// <param name="protocol"></param>
        /// <param name="targetPort"></param>
        /// <param name="localPort"></param>
        /// 
        /// <returns>false if item is blocked and was thus not added to internal query list</returns>
        internal bool AddItem(int pid, string path, string target, int protocol, int targetPort, int localPort)
        {
            try
            {
                string fileName = System.IO.Path.GetFileName(path);
                if (path != "System")
                {
                    path = Common.IO.Files.PathResolver.GetFriendlyPath(path);
                }

                //FIXME: Do a proper path compare...? CASE!
                var existing = this.Connections.FirstOrDefault(c => c.CurrentPath == path && c.Target == target && c.TargetPort == targetPort.ToString() && (localPort >= IPHelper.GetMaxUserPort() || c.LocalPort == localPort.ToString()) && c.Protocol == protocol);
                if (existing != null)
                {
                    LogHelper.Debug("Connection matches an already existing connection request.");
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
                    string[] svc = Array.Empty<string>();
                    string[] svcdsc = Array.Empty<string>();
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
                                // TODO: this happens when accessing system32 files from a x86 application i.e. File.Exists always returns false; solution would be to target AnyCPU
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
                            ServiceInfoResult svcInfo = AsyncTaskRunner.GetServiceInfo(pid, fileName);
                            if (svcInfo != null)
                            {
                                svc = new string[] { svcInfo.Name };
                                svcdsc = new string[] { svcInfo.DisplayName };
                                unsure = false;
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
                    var blockingRules = FirewallHelper.GetMatchingRules(path, ProcessHelper.GetAppPkgId(pid), protocol, target, targetPort.ToString(), localPort.ToString(), unsure ? svc : svc.Take(1), ProcessHelper.GetLocalUserOwner(pid), blockOnly:true, outgoingOnly:true);
                    if (blockingRules.Any())
                    {
                        LogHelper.Info("Connection matches a block-rule!");

                        StringBuilder sb = new StringBuilder();
                        sb.Append("Blocked by: ");
                        foreach (Rule s in blockingRules)
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
                        CurrentAppPkgId = ProcessHelper.GetAppPkgId(pid),
                        CurrentLocalUserOwner = ProcessHelper.GetLocalUserOwner(pid),
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

        
        private static void ResolveHostForConnection(CurrentConn conn)
        {
            try
            {
                _ = DnsResolver.ResolveIpAddress(conn.Target, entry => conn.ResolvedHost = conn.Target != entry.HostEntry.HostName ? entry.HostEntry.HostName : "..."); 
            }
            catch (Exception e) 
            {
                LogHelper.Warning($"Cannot resolve host name for {conn.Target} - Exception: {e.Message}");
            }
        }

        public void Dispose()
        {
            asyncTaskRunner?.Dispose();
        }
    }
}
