using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Notifier.Helpers;
using Wokhan.WindowsFirewallNotifier.Notifier.UI.Windows;

namespace Wokhan.WindowsFirewallNotifier.Notifier
{
    internal class AsyncTaskRunner : IDisposable
    {
        private readonly CancellationTokenSource _eventLogPollingTaskCancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationTokenSource _updateServiceTaskCancellationTokenSource = new CancellationTokenSource();

        internal static Dictionary<int, ServiceInfoResult> SERVICES = ProcessHelper.GetAllServicesByPidWMI();

        private readonly App _application;

        internal AsyncTaskRunner(App application)
        {
            _application = application;
        }

        internal void StartTasks()
        {
            LogHelper.Debug("Start security log polling task...");
            _ = EventLogPollingTaskAsync(1_000);

            LogHelper.Debug("Start update services task...");
            _ = UpdateServiceInfoTaskAsync(30_000);
        }

        private async Task UpdateServiceInfoTaskAsync(int waitMillis)
        {
            try
            {
                LogHelper.Info($"Start update service info task ...");
                DateTime timeStamp = DateTime.Now;
                CancellationToken cancellationToken = _updateServiceTaskCancellationTokenSource.Token;
                while (true)
                {
                    CheckCancelTaskRequestedAndThrow(cancellationToken);
                    await Task.Delay(waitMillis, cancellationToken).ConfigureAwait(false);
                    Dictionary<int, ServiceInfoResult> dict = ProcessHelper.GetAllServicesByPidWMI();
                    SERVICES = dict;
                    LogHelper.Debug($"Service info updated");
                }
            }
            catch (OperationCanceledException e)
            {
                LogHelper.Info($"UpdateServiceInfoTask cancelled: {e.Message}");
            }
            catch (Exception e)
            {
                LogHelper.Error($"UpdateServiceInfoTask exception", e);
                MessageBox.Show($"UpdateServiceInfoTask exception:\n{e.Message}", "Update service info exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task EventLogPollingTaskAsync(int waitMillis)
        {
            try
            {
                LogHelper.Info($"Start security event log polling ...");
                DateTime lastLogEntryTimeStamp = DateTime.Now;
                CancellationToken cancellationToken = _eventLogPollingTaskCancellationTokenSource.Token;
                while (true)
                {
                    try
                    {
                        using var securityLog = new EventLog("security");
                        List<EventLogEntry> newEntryList = new List<EventLogEntry>();
                        int entryIndex = securityLog.Entries.Count - 1;
                        DateTime newestEntryTimeWritten = securityLog.Entries[entryIndex].TimeWritten;
                        for (int i = entryIndex; i >= 0; i--)
                        {
                            CheckCancelTaskRequestedAndThrow(cancellationToken);
                            EventLogEntry entry = securityLog.Entries[i];
                            bool isNewEntry = entry.TimeWritten > lastLogEntryTimeStamp;
                            if (isNewEntry)
                            {
                                if (IsEventInstanceIdAccepted(entry.InstanceId))
                                {
                                    WPFUtils.DispatchUI(() => App.GetActivityWindow().ShowActivity(ActivityWindow.ActivityEnum.Blocked));
                                    newEntryList.Insert(0, entry);
                                }
                                else
                                {
                                    WPFUtils.DispatchUI(() => App.GetActivityWindow().ShowActivity(ActivityWindow.ActivityEnum.Allowed));
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        lastLogEntryTimeStamp = newestEntryTimeWritten;

                        foreach (EventLogEntry entry in newEntryList)
                        {
                            CheckCancelTaskRequestedAndThrow(cancellationToken);
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                    // dispatch to ui thread
                                    _application.HandleEventLogNotification(entry);
                            });
                        }
                    }
                    catch (ArgumentException e)
                    {
                        LogHelper.Warning($"Security log entry does not exist anymore:" + e.Message);
                    }
                    CheckCancelTaskRequestedAndThrow(cancellationToken);
                    await Task.Delay(waitMillis, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (SecurityException se)
            {
                LogHelper.Error($"Notifier cannot access security event log: { se.Message}. Notifier needs to be started with admin rights and will exit now", se);
                MessageBox.Show($"Notifier cannot access security event log:\n{se.Message}\nNotifier needs to be started with admin rights.\nNotifier will exit.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this._application.Shutdown();
            }
            catch (Exception e)
            {
                LogHelper.Error("EventLogPollingTaskAsync exception: " + e.Message, e);
                MessageBox.Show($"Security event log polling exception:\n{e.Message}\nNotifier will exit", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                this._application.Shutdown();
            }
        }

        private void CheckCancelTaskRequestedAndThrow(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        internal void CancelTasks()
        {
            LogHelper.Debug($"AsyncTaskRunner: CancelTasks requested...");
            if (_eventLogPollingTaskCancellationTokenSource != null)
            {
                _eventLogPollingTaskCancellationTokenSource.Cancel();
            }
            if (_updateServiceTaskCancellationTokenSource != null)
            {
                _updateServiceTaskCancellationTokenSource.Cancel();
            }
        }

        internal static string GetServicName(int pid)
        {
            return SERVICES.ContainsKey(pid) ? SERVICES[pid].Name : "-";
        }

        internal static ServiceInfoResult GetServiceInfo(int pid, string fileName)
        {
            if (SERVICES.TryGetValue(pid, out ServiceInfoResult svcInfo))
            {
                LogHelper.Debug($"Service detected for '{fileName}': '{svcInfo.Name}'");
                return svcInfo;
            }
            else
            {
                //ProcessHelper.GetService(pid, threadid, path, protocol, localPort, target, targetPort, out svc, out svcdsc, out unsure);
                LogHelper.Debug($"No service detected for '{fileName}'");
                return null;
            }
        }


        internal static Boolean IsEventInstanceIdAccepted(long instanceId)
        {
            // https://docs.microsoft.com/en-us/windows/security/threat-protection/auditing/audit-filtering-platform-connection
            return
                instanceId == 5157 // block connection
                || instanceId == 5152 // drop packet
                ;
        }

        public void Dispose()
        {
            LogHelper.Debug($"AsyncTaskRunner: Disposing resources...");
            if (_eventLogPollingTaskCancellationTokenSource != null)
            {
                _eventLogPollingTaskCancellationTokenSource.Dispose();
            }
            if (_updateServiceTaskCancellationTokenSource != null)
            {
                _updateServiceTaskCancellationTokenSource.Dispose();
            }
        }
    }
}
