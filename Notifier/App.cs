
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Security.Principal;
using System.Threading;
using System.Windows;

using Wokhan.WindowsFirewallNotifier.Common.Config;
using Wokhan.WindowsFirewallNotifier.Common.Logging;
using Wokhan.WindowsFirewallNotifier.Common.Net.IP;
using Wokhan.WindowsFirewallNotifier.Common.Net.WFP;
using Wokhan.WindowsFirewallNotifier.Common.Processes;
using Wokhan.WindowsFirewallNotifier.Common.Security;
using Wokhan.WindowsFirewallNotifier.Common.UI.ViewModels;
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
        private static App APP_INSTANCE;
        private static NotificationWindow notifierWindow;
        private static ActivityWindow activityWindow;
        private EventLogAsyncReader<LogEntryViewModel> eventLogListener;

        public ObservableCollection<CurrentConn> Connections { get; } = new ObservableCollection<CurrentConn>();

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
                    MessageBox.Show($"User must have admin rights to run Notifier\nNotifier will exit now!", "Security check", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(1);
                }

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
                    LogHelper.Warning("A notififer instance is already running - showing it.");
                    //MessageBox.Show("A notifier instance is already running");
                    APP_INSTANCE.ShowNotifierWindow();  // FIXME: show it - seems not to work as it should
                }
            }
            catch (Exception e)
            {
                // use log4net directly in case LogHelper throws an exception itself during startup
                LogHelper.Error(e.Message, e);
                Environment.Exit(1);
            }
            Environment.Exit(0);
        }

        public App() : base()
        {
            this.ShutdownMode = ShutdownMode.OnMainWindowClose;

#if DEBUG
            this.Connections.Add(AppDataSample.DemoConnection);
#endif

            LogHelper.Info("Init notification window...");
            notifierWindow = new NotificationWindow
            {
                WindowState = WindowState.Normal
            };
            MainWindow = notifierWindow;
            activityWindow = new ActivityWindow(notifierWindow, Settings.Default.ActivityWindow_Shown);

            try
            {
                eventLogListener = new EventLogAsyncReader<LogEntryViewModel>(EventLogAsyncReader.EVENTLOG_SECURITY, LogEntryViewModel.CreateFromEventLogEntry);
                eventLogListener.FilterPredicate = EventLogAsyncReader.IsFirewallEventSimple;
                eventLogListener.EntryWritten += HandleEventLogNotification;
            }
            catch (SecurityException se)
            {
                LogHelper.Error($"Notifier cannot access security event log: {se.Message}. Notifier needs to be started with admin rights and will exit now", se);
                MessageBox.Show($"Notifier cannot access security event log:\n{se.Message}\nNotifier needs to be started with admin rights.\nNotifier will exit.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
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
            notifierWindow?.Close();
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

        internal void HandleEventLogNotification(object sender, EntryWrittenEventArgs eventArgs)
        {
            var entry = eventArgs.Entry;
            bool allowed = EventLogAsyncReader.IsFirewallEventAllowed(entry.InstanceId);
            activityWindow.ShowActivity(allowed ? ActivityWindow.ActivityEnum.Allowed : ActivityWindow.ActivityEnum.Blocked);
            if (allowed || !LogEntryViewModel.TryCreateFromEventLogEntry(entry, 0, out CurrentConn view))
            {
                return;
            }

            LogHelper.Info($"Handle {view.Direction}-going connection for '{view.FileName}', service: {view.ServiceName} ...");
            if (!AddItem(view))
            {
                //This connection is blocked by a specific rule. No action necessary.
                LogHelper.Info($"{view.Direction}-going connection for '{view.FileName}' is blocked by a rule - ignored.");
                return;
            }

            //if (notifierWindow.WindowState == WindowState.Minimized)
            //{
            //    notifierWindow.ShowActivityTrayIcon($"Notifier blocked connections - click tray icon to show");  // max 64 chars!
            //}
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
        internal bool AddItem(CurrentConn conn)
        {
            try
            {
                var sourcePortAsInt = int.Parse(conn.SourcePort);
                var existing = Dispatcher.Invoke(() => this.Connections.FirstOrDefault(c => StringComparer.InvariantCultureIgnoreCase.Equals(c.Path, conn.Path) && c.TargetIP == conn.TargetIP && c.TargetPort == conn.TargetPort && (sourcePortAsInt >= IPHelper.GetMaxUserPort() || c.SourcePort == conn.SourcePort) && c.RawProtocol == conn.RawProtocol));
                if (existing != null)
                {
                    LogHelper.Debug("Connection matches an already existing connection request.");
                    if (!existing.LocalPortArray.Contains(sourcePortAsInt))
                    {
                        existing.LocalPortArray.Add(sourcePortAsInt);
                        //Note: Unfortunately, C# doesn't have a simple List that automatically sorts... :(
                        // TODO: it does with SortedSet. Don't get this comment...
                        // existing.LocalPortArray.Sort();
                        existing.SourcePort = IPHelper.MergePorts(existing.LocalPortArray);
                    }
                    existing.TentativesCounter++;
                }
                else
                {
                    ServiceInfoResult svcInfo = null;
                    if (Settings.Default.EnableServiceDetection)
                    {
                        svcInfo = ServiceNameResolver.GetServiceInfo(conn.Pid, conn.FileName);
                    }

                    conn.CurrentAppPkgId = ProcessHelper.GetAppPkgId(conn.Pid);
                    conn.CurrentLocalUserOwner = ProcessHelper.GetLocalUserOwner(conn.Pid);
                    conn.CurrentService = svcInfo?.DisplayName;
                    conn.CurrentServiceDesc = svcInfo?.Name;
                    // Check whether this connection is blocked by a rule.
                    var blockingRules = FirewallHelper.GetMatchingRules(conn.Path, conn.CurrentAppPkgId, conn.RawProtocol, conn.TargetIP, conn.TargetPort, conn.SourcePort, conn.CurrentServiceDesc, conn.CurrentLocalUserOwner, blockOnly: true, outgoingOnly: true);
                    if (blockingRules.Any())
                    {
                        LogHelper.Info("Connection matches a block-rule!");

                        LogHelper.Debug($"pid: {Process.GetCurrentProcess().Id} GetMatchingRules: {conn.FileName}, {conn.Protocol}, {conn.TargetIP}, {conn.TargetPort}, {conn.SourcePort}, {svcInfo?.Name}");

                        return false;
                    }


                    conn.LocalPortArray.Add(sourcePortAsInt);

                    Dispatcher.Invoke(() => this.Connections.Add(conn));

                    return true;
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to add the connection to the pool.", e);
            }

            return false;
        }


        public void Dispose()
        {
            eventLogListener?.Dispose();
        }
    }
}
