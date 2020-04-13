using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Wokhan.WindowsFirewallNotifier.Common.Config;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Common.IO.Files;
using Wokhan.WindowsFirewallNotifier.Common.Net.WFP;
using Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Interaction logic for EventLog.xaml
    /// </summary>
    public sealed partial class EventsLog : Page, INotifyPropertyChanged, IDisposable
    {
        private readonly Dictionary<int, ServiceInfoResult> services = ProcessHelper.GetAllServicesByPidWMI();
        internal readonly ICollectionView dataView;
        private readonly EventsLogFilters eventsLogFilters;

        private static readonly ToolTip toolTipInstance = new ToolTip
        {
            Content = "",
            PlacementTarget = null,
            StaysOpen = true,
            IsOpen = false
        };

        private EventLog securityLog;

        public ObservableCollection<LogEntryViewModel> LogEntries { get; } = new ObservableCollection<LogEntryViewModel>();
        //public List<LogEntryViewModel> TempEntries { get; } = new List<LogEntryViewModel>();

        private int _scanProgress;
        public int ScanProgress
        {
            get => _scanProgress;
            set
            {
                // TODO: replace with helpers from Wokhan libraries (once they are moved to github)
                if (_scanProgress != value)
                {
                    _scanProgress = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ScanProgress)));
                }
            }
        }

        private int _scanProgressMax;
        public int ScanProgressMax
        {
            get => _scanProgressMax;
            set
            {
                // TODO: replace with helpers from Wokhan libraries (once they are moved to github)
                if (_scanProgressMax != value)
                {
                    _scanProgressMax = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ScanProgressMax)));
                }
            }
        }

        private bool _isTrackingEnabled = true;
        public bool IsTrackingEnabled
        {
            get => _isTrackingEnabled;
            set
            {
                _isTrackingEnabled = value;
                if (_isTrackingEnabled)
                {
                    StartHandlingSecurityLogEvents();
                }
                else
                {
                    PauseHandlingSecurityLogEvents();
                }

                // Notify xaml data trigger
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsTrackingEnabled)));
            }
        }

        private bool _refreshRulesFilterData = true;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsTCPOnlyEnabled
        {
            get => Settings.Default.FilterTcpOnlyEvents;
            set
            {
                if (IsTCPOnlyEnabled != value)
                {
                    Settings.Default.FilterTcpOnlyEvents = value;
                    Settings.Default.Save();
                    eventsLogFilters.ResetTcpFilter();
                }
            }
        }
        public string FilterText
        {
            get => eventsLogFilters.FilterText;
            set
            {
                eventsLogFilters.FilterText = value;
                eventsLogFilters.ResetTextfilter();
            }
        }

        private readonly object EntriesLock = new object();

        int SecurityLogEntryIndex_last { get; set; }

        public EventsLog()
        {
            eventsLogFilters = new EventsLogFilters(this);

            InitializeComponent();

            if (!Settings.Default.EnableDnsResolver)
            {
                RemoteHostCol.Visibility = Visibility.Hidden;
            }

            dataView = CollectionViewSource.GetDefaultView(gridLog.ItemsSource);
            dataView.SortDescriptions.Add(new SortDescription(nameof(LogEntryViewModel.Timestamp), ListSortDirection.Descending));

            Loaded += (s, e) => StartHandlingSecurityLogEvents();
            Unloaded += (s, e) => StopHandlingSecurityLogEvents();
        }

        private void StartHandlingSecurityLogEvents()
        {
            try
            {
                if (securityLog is null)
                {
                    securityLog = new EventLog("security");
                }
                securityLog.BeginInit();
                Dispatcher.Invoke(() => { ScanProgressMax = securityLog.Entries.Count; });
                eventsLogFilters.ResetTcpFilter();
                SecurityLogEntryIndex_last = securityLog.Entries.Count - 1;
                _ = Task.Run(() => { InitEventLog(SecurityLogEntryIndex_last, value => ScanProgress = value); }).ConfigureAwait(false);
                securityLog.EntryWritten += SecurityLog_EntryWritten;
                securityLog.EnableRaisingEvents = true;
                securityLog.EndInit();

            }
            catch (Exception exc)
            {
                LogHelper.Error("Unable to connect to the event log", exc);
                throw;
            }

        }
        private void PauseHandlingSecurityLogEvents()
        {
            securityLog.EnableRaisingEvents = false;
        }
        private void StopHandlingSecurityLogEvents()
        {
            PauseHandlingSecurityLogEvents();
            securityLog?.Dispose();
        }

        private void SecurityLog_EntryWritten(object sender, EntryWrittenEventArgs e)
        {
            //LogHelper.Debug($"EntryWritten event ...");
            if (!FirewallHelper.IsEventAccepted(e.Entry))
            {
                return;
            }

            lock (EntriesLock)
            {
                if (e.Entry != null && e.Entry.Index > SecurityLogEntryIndex_last)
                {
                    LogEntryViewModel entry = EntryViewFromLogEntry(e.Entry);
                    if (entry != null)
                    {
                        Dispatcher.Invoke(() => { LogEntries.Add(entry); ScanProgress = LogEntries.Count; });
                    }
                }
                if (ScanProgressMax < LogEntries.Count)
                {
                    // overflow handling
                    LogEntries.RemoveAt(0);
                }
                SecurityLogEntryIndex_last = securityLog.Entries.Count - 1;
            }
        }

        /*
         * Initializes the LogEntries list to show the most recent events
         */
        private void InitEventLog(int lastIndex, Action<int> progressCallback)
        {
            int i = 0;
            try
            {
                LogHelper.Debug($"InitEventLog ...");
                for (i = lastIndex; i >= 0; i--)
                {
                    lock (EntriesLock)
                    {
                        if (i > securityLog.Entries.Count - 1) { continue; }
                        EventLogEntry entry = securityLog.Entries[i];
                        if (FirewallHelper.IsEventAccepted(entry))
                        {
                            LogEntryViewModel entryView = EntryViewFromLogEntry(entry);
                            if (entryView != null)
                            {
                                Dispatcher.Invoke(() => { LogEntries.Insert(0, entryView); progressCallback?.Invoke(LogEntries.Count); });
                            }
                        }
                    }
                }
                LogHelper.Debug($"InitEventLog - Logentries.count={LogEntries.Count}");
            }
            catch (ObjectDisposedException ede)
            {
                // Could use a cancellation token instead, but chances are that dispose will occur before the token is actually checked. Going for the ugly way then.
                // LogHelper.Error($"{ede.Message} - ignored", ede);
            }
            catch (IndexOutOfRangeException oex)
            {
                LogHelper.Error($"Unable to load the event log entry: index={i} lastIndex={lastIndex} count={securityLog?.Entries?.Count} - ignored", oex);
            }
            catch (Exception exc)
            {
                LogHelper.Error($"Unable to load the event log entry: index={i} lastIndex={lastIndex} count={securityLog?.Entries?.Count} - thrown", exc);
                throw;
            }
        }

        private LogEntryViewModel EntryViewFromLogEntry(EventLogEntry entry)
        {
            try
            {
                //LogHelper.Debug($"Create EntryViewModel entry...");
                var pid = int.Parse(GetReplacementString(entry, 0));
                var direction = GetReplacementString(entry, 2) == @"%%14593" ? "Out" : "In";
                string targetIp;
                string targetPort;
                if (direction == "Out")
                {
                    // outgoing target ip
                    targetIp = GetReplacementString(entry, 5);
                    targetPort = GetReplacementString(entry, 6);
                }
                else
                {
                    // incoming source ip
                    targetIp = GetReplacementString(entry, 3);
                    targetPort = GetReplacementString(entry, 4);
                }

                var friendlyPath = GetReplacementString(entry, 1) == "-" ? "System" : PathResolver.GetFriendlyPath(GetReplacementString(entry, 1));
                var fileName = System.IO.Path.GetFileName(friendlyPath);
                var protocol = int.Parse(GetReplacementString(entry, 7));

                // try to get the servicename from pid (works only if service is running)
                var serviceName = services.ContainsKey(pid) ? services[pid].Name : "-";

                var le = new LogEntryViewModel()
                {
                    Id = entry.Index,
                    Pid = pid,
                    Timestamp = entry.TimeGenerated,
                    Path = GetReplacementString(entry, 1) == "-" ? "System" : GetReplacementString(entry, 1),
                    FriendlyPath = friendlyPath,
                    ServiceName = serviceName,
                    FileName = fileName,
                    TargetIP = targetIp,
                    TargetPort = targetPort,
                    Protocol = Protocol.GetProtocolAsString(protocol),
                    Direction = direction,
                    FilterId = GetReplacementString(entry, 8),
                    Reason = FirewallHelper.GetEventInstanceIdAsString(entry.InstanceId),
                    Reason_Info = entry.Message
                };

                le.ReasonColor = le.Reason.StartsWith("Block") ? Brushes.OrangeRed : Brushes.Blue;
                le.DirectionColor = le.Direction.StartsWith("In") ? Brushes.OrangeRed : Brushes.Black;

                return le;
            }
            catch (Exception ex)
            {
                LogHelper.Error("Cannot parse eventlog entry: eventID=" + entry.InstanceId.ToString(), ex);
            }

            return null;
        }

        private static string GetReplacementString(EventLogEntry entry, int i) => entry.ReplacementStrings.DefaultIfEmpty(string.Empty).ElementAtOrDefault(i);

        private void btnLocate_Click(object sender, RoutedEventArgs e)
        {
            var selectedLog = (LogEntryViewModel)gridLog.SelectedItem;
            if (selectedLog is null)
            {
                //@
                return;
            }
            ProcessHelper.StartShellExecutable("explorer.exe", "/select," + selectedLog.FriendlyPath, true);
        }

        private void btnEventLogVwr_Click(object sender, RoutedEventArgs e)
        {
            ProcessHelper.StartShellExecutable("eventvwr.msc", null, true);
        }

        private void btnRestartAdmin_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).RestartAsAdmin();
        }

        private void GridLog_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //System.Console.WriteLine($"Grid SelectionChanged: {sender}, {e.Source}, {e.Handled}, {e.OriginalSource}, {e}");
            if (gridLog.SelectedItem is null)
            {
                btnLocate.IsEnabled = false;
            }
            else
            {
                btnLocate.IsEnabled = true;
                if ((bool)btnAutoRefreshToggle.IsChecked)
                {
                    // disable the auto-refresh for not loosing the selection to locate
                    btnAutoRefreshToggle.IsChecked = false;
                }
            }
        }

        private void Reason_GotFocus(object sender, RoutedEventArgs e)
        {
            // System.Console.WriteLine($"Columng Reason GotFocus: {sender}, {e.Source}, {e.Handled}, {e.OriginalSource}, {e}");
        }

        private void GridLog_CellSelected(object sender, RoutedEventArgs e)
        {
            //System.Console.WriteLine($"CellSelected: {sender}, {e.Source}, {e.Handled}, {e.OriginalSource}, {e}");
            ShowMatchingRuleAndDetails((DataGrid)e.Source, (DataGridCell)e.OriginalSource);  // case when selection changed
        }

        private void GridLog_GotFocus(object sender, RoutedEventArgs e)
        {
            //System.Console.WriteLine($"Cell GotFocus: {sender}, {e.Source}, {e.Handled}, {e.OriginalSource}, {e}");
            ShowMatchingRuleAndDetails((DataGrid)e.Source, (DataGridCell)e.OriginalSource);  // case when row already selected and cell got focus
        }

        private void ShowMatchingRuleAndDetails(DataGrid grid, DataGridCell cell)
        {
            var selectedEntry = (LogEntryViewModel)grid.SelectedItem;
            if (selectedEntry != null && Reason.Equals(cell.Column) && cell.IsFocused && cell.IsSelected)
            {
                // Filter which blocked the connection
                string matchingFilterDetails;
                try
                {
                    FilterResult matchingFilter = NetshHelper.FindMatchingFilterInfo(int.Parse(selectedEntry.FilterId), refreshData: _refreshRulesFilterData);
                    _refreshRulesFilterData = false;
                    var filterInfo = WrapTextTrunc($"{ matchingFilter.Name} - { matchingFilter.Description}", 120, "\t");
                    matchingFilterDetails = matchingFilter != null ? $"\n\n" +
                        $"Filter rule which triggered the event:\n" +
                        $"\t{selectedEntry.FilterId}: {filterInfo}\n" :
                        "\n\n... No filter rule found ...";
                }
                catch (Exception ex)
                {
                    LogHelper.Warning("Cannot get filter rule:" + ex.Message);
                    matchingFilterDetails = $"\n\n" +
                        $"Cannot get filter rule: {ex.Message}";
                }

                //// Other matching filters for process
                //IEnumerable<FirewallHelper.Rule> rules = FirewallHelper.GetMatchingRulesForEvent(int.Parse(selectedEntry.Pid), selectedEntry.Path, selectedEntry.TargetIP, selectedEntry.TargetPort, blockOnly: false, outgoingOnly: false);
                //string reasonDetails = $"\nMatching Rules for | {selectedEntry.FileName} | {selectedEntry.Pid} | {selectedEntry.TargetIP}:{selectedEntry.TargetPort} |";
                //foreach (FirewallHelper.Rule rule in rules.Take(10))
                //{
                //    reasonDetails += $"\n'{rule.Name}' | {rule.ActionStr} | {rule.DirectionStr} | {rule.AppPkgId} | profile={rule.ProfilesStr} | svc={rule.ServiceName} | {System.IO.Path.GetFileName(rule.ApplicationName)}";
                //}
                //if (rules.Count() > 10)
                //{
                //    reasonDetails += "\n...more...";
                //}
                //else if (rules.Count() == 0)
                //{
                //    reasonDetails += "\n... no matching rules found ...";
                //}
                var serviceNameInfo = !string.IsNullOrEmpty(selectedEntry.ServiceName) ? $"{selectedEntry.ServiceName}" : "-";
                ShowToolTip(cell, selectedEntry.Reason_Info + $"\n\nService:\t{serviceNameInfo}" + matchingFilterDetails); // + reasonDetails);
            }
            else
            {
                CloseToolTip();
            }
        }

        private static string WrapTextTrunc(string text, int maxChars, string indent = " ")
        {
            if (text.Length > maxChars)
            {
                return Regex.Replace(text, "(.{" + maxChars + "})", "$1\n" + indent);
            }
            return text;
        }

        private void ShowToolTip(UIElement placementTarget, string text)
        {
            toolTipInstance.PlacementTarget = placementTarget;
            toolTipInstance.Content = text;
            toolTipInstance.IsOpen = true;
            placementTarget.LostFocus += PlacementTarget_LostFocus;
        }

        private static void CloseToolTip()
        {
            toolTipInstance.Content = "";
            toolTipInstance.IsOpen = false;
            toolTipInstance.PlacementTarget = null;
        }

        private void PlacementTarget_LostFocus(object sender, RoutedEventArgs e)
        {
            CloseToolTip();
            (sender as UIElement).LostFocus -= PlacementTarget_LostFocus;
        }

        private void ShowToolTip(Control control)
        {
            // shows the controls tooltip on demand
            if (control.ToolTip != null)
            {
                if (control.ToolTip is ToolTip castToolTip)
                {
                    castToolTip.IsOpen = true;
                }
                else
                {
                    _ = new ToolTip
                    {
                        Content = control.ToolTip,
                        StaysOpen = false,
                        IsOpen = true
                    };
                }
            }
        }

        private class EntryComparer : IEqualityComparer<LogEntryViewModel>
        {
            public static IEqualityComparer<LogEntryViewModel> Instance { get; } = new EntryComparer();

            public bool Equals([AllowNull] LogEntryViewModel x, [AllowNull] LogEntryViewModel y) => x.Id == y.Id;

            public int GetHashCode([DisallowNull] LogEntryViewModel obj) => obj.Id;
        }

        public void Dispose()
        {
            securityLog?.Dispose();
        }
    }
}
