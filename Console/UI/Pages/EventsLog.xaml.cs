using AlphaChiTech.Virtualization;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

using Wokhan.ComponentModel.Extensions;
using Wokhan.UI.Extensions;
using Wokhan.WindowsFirewallNotifier.Common.Config;
using Wokhan.WindowsFirewallNotifier.Common.Logging;
using Wokhan.WindowsFirewallNotifier.Common.Net.WFP;
using Wokhan.WindowsFirewallNotifier.Common.Processes;
using Wokhan.WindowsFirewallNotifier.Common.Security;
using Wokhan.WindowsFirewallNotifier.Common.UI.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Interaction logic for EventLog.xaml
    /// </summary>
    public sealed partial class EventsLog : Page, INotifyPropertyChanged, IDisposable
    {
        private readonly EventsLogFilters eventsLogFilters;

        private static readonly ToolTip toolTipInstance = new ToolTip
        {
            Content = "",
            PlacementTarget = null,
            StaysOpen = true,
            IsOpen = false
        };

        private EventLogAsyncReader<LogEntryViewModel> LogListener;

        private int _scanProgress;
        public int ScanProgress
        {
            get => _scanProgress;
            set => this.SetValue(ref _scanProgress, value, NotifyPropertyChanged);
        }

        private int _scanProgressMax;
        public int ScanProgressMax
        {
            get => _scanProgressMax;
            set => this.SetValue(ref _scanProgressMax, value, NotifyPropertyChanged);
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

        private EventLogAsyncReader<LogEntryViewModel> virtualizedLogEntries;
        public EventLogAsyncReader<LogEntryViewModel> eventsReader
        {
            get => virtualizedLogEntries;
            private set => this.SetValue(ref virtualizedLogEntries, value, NotifyPropertyChanged);
        }

        public EventsLog()
        {
            VirtualizedQueryableExtensions.Init(Dispatcher);

            Loaded += (s, e) => StartHandlingSecurityLogEvents();
            Unloaded += (s, e) => StopHandlingSecurityLogEvents();

            eventsLogFilters = new EventsLogFilters(this);

            InitializeComponent();

            if (!Settings.Default.EnableDnsResolver)
            {
                RemoteHostCol.Visibility = Visibility.Hidden;
            }
        }

        private void StartHandlingSecurityLogEvents()
        {
            try
            {
                if (eventsReader is null)
                {
                    eventsReader = new EventLogAsyncReader<LogEntryViewModel>(EventLogAsyncReader.EVENTLOG_SECURITY, LogEntryViewModel.CreateFromEventLogEntry);
                    gridLog.ItemsSource = eventsReader.Entries;
                }

                ScanProgressMax = eventsReader.Count;

                eventsLogFilters.ResetTcpFilter();
            }
            catch (Exception exc)
            {
                LogHelper.Error("Unable to connect to the event log", exc);
                throw;
            }
        }

        private void PauseHandlingSecurityLogEvents()
        {
            LogListener.StopRaisingEvents();
        }

        private void StopHandlingSecurityLogEvents()
        {
            PauseHandlingSecurityLogEvents();

            LogListener?.Dispose();
            LogListener = null;
        }

        private void SecurityLog_EntryWritten(EventLogEntry entry, bool allowed)
        {
            //if (source.CurrentPageOffset == 0)
            {
                //VirtualizedLogEntries.ResetAsync();
            }
        }
        private void btnLocate_Click(object sender, RoutedEventArgs e)
        {
            var selectedLog = (LogEntryViewModel)gridLog.SelectedItem;
            if (selectedLog is null)
            {
                //@
                return;
            }
            ProcessHelper.StartShellExecutable("explorer.exe", "/select," + selectedLog.Path, true);
        }

        private void btnEventLogVwr_Click(object sender, RoutedEventArgs e)
        {
            ProcessHelper.StartShellExecutable("eventvwr.msc", null, true);
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

        public void Dispose()
        {
            LogListener?.Dispose();
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
