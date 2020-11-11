using System;
using System.Diagnostics;
using System.Security;
using System.Windows;

using Wokhan.WindowsFirewallNotifier.Common.Logging;
using Wokhan.WindowsFirewallNotifier.Notifier.UI.Windows;

namespace Wokhan.WindowsFirewallNotifier.Notifier
{
    internal class EventLogListener : IDisposable
    {
        private readonly App _application;

        private EventLog securityLog;

        internal EventLogListener(App application)
        {
            _application = application;

            try
            {
                securityLog = new EventLog("security") { EnableRaisingEvents = true };
                securityLog.EntryWritten += SecurityLog_EntryWritten;
            }
            catch (SecurityException se)
            {
                LogHelper.Error($"Notifier cannot access security event log: { se.Message}. Notifier needs to be started with admin rights and will exit now", se);
                MessageBox.Show($"Notifier cannot access security event log:\n{se.Message}\nNotifier needs to be started with admin rights.\nNotifier will exit.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this._application.Shutdown();
            }
        }

        private void SecurityLog_EntryWritten(object sender, EntryWrittenEventArgs e)
        {
            var entry = e.Entry;
            if (IsEventInstanceIdAccepted(entry.InstanceId))
            {
                App.GetActivityWindow().ShowActivity(ActivityWindow.ActivityEnum.Blocked);
                _application.HandleEventLogNotification(entry);
            }
            else
            {
                App.GetActivityWindow().ShowActivity(ActivityWindow.ActivityEnum.Allowed);
            }
        }

        internal static bool IsEventInstanceIdAccepted(long instanceId)
        {
            // https://docs.microsoft.com/en-us/windows/security/threat-protection/auditing/audit-filtering-platform-connection
            return instanceId == 5157 // block connection
                || instanceId == 5152;// drop packet
        }

        public void Dispose()
        {
            LogHelper.Debug($"AsyncTaskRunner: Disposing resources...");
            if (securityLog != null)
            {
                securityLog.Dispose();
            }
        }
    }
}
