using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Interaction logic for EventLog.xaml
    /// </summary>
    public partial class EventsLog : Page
    {
        private const int MaxEventsToLoad = 500;

        public bool IsTrackingEnabled
        {
            get { return timer.IsEnabled; }
            set { timer.IsEnabled = value; }
        }

        private DispatcherTimer timer = new DispatcherTimer() { IsEnabled = true };

        public List<int> Intervals { get { return new List<int> { 1, 5, 10 }; } }

        private int _interval = 1;
        public int Interval
        {
            get { return _interval; }
            set { _interval = value; timer.Interval = TimeSpan.FromSeconds(value); }
        }

        public EventsLog()
        {
            InitializeComponent();

            if (((App)Application.Current).IsElevated)
            {
                timer.Interval = TimeSpan.FromSeconds(Interval);
                timer.Tick += timer_Tick;

                this.Loaded += EventsLog_Loaded;
                this.Unloaded += EventsLog_Unloaded;
            }
        }

        private void EventsLog_Unloaded(object sender, RoutedEventArgs e)
        {
            timer.Stop();
        }

        void EventsLog_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(() => timer_Tick(null, null));
        }

        void timer_Tick(object sender, EventArgs e)
        {
            initEventLog();
        }


        private ObservableCollection<LogEntryViewModel> _logEntries = new ObservableCollection<LogEntryViewModel>();
        public ObservableCollection<LogEntryViewModel> LogEntries { get { return _logEntries; } }

        private DateTime lastDate = DateTime.MinValue;
        private int lastIndex = 0;
        private void initEventLog()
        {
            try
            {
                using (EventLog securityLog = new EventLog("security"))
                {
                    int i = securityLog.Entries.Count - 1;
                    int cpt = MaxEventsToLoad;
                    bool isAppending = _logEntries.Any();
                    DateTime lastDateNew = DateTime.MinValue;
                    int indexNew = 0;

                    while (i >= 0)
                    {
                        EventLogEntry entry = securityLog.Entries[i];
                        i--;

                        if (lastDate != DateTime.MinValue && entry.TimeWritten <= lastDate && (entry.Index == lastIndex || lastIndex == -1))
                        {
                            break;
                        }

                        if (lastDateNew == DateTime.MinValue)
                        {
                            // Store where we start processing entries.
                            lastDateNew = entry.TimeWritten;
                            indexNew = entry.Index;
                        }

                        // Note: instanceId == eventID
                        if (entry.EntryType == EventLogEntryType.FailureAudit &&
                            FirewallHelper.isEventInstanceIdAccepted(entry.InstanceId))
                        {
                            cpt--;
                            string friendlyPath = FileHelper.GetFriendlyPath(entry.ReplacementStrings[1]);
                            var le = new LogEntryViewModel()
                            {
                                Pid = entry.ReplacementStrings[0],
                                Timestamp = entry.TimeGenerated,
                                Icon = IconHelper.GetIcon(entry.ReplacementStrings[1]),
                                Path = entry.ReplacementStrings[1],
                                FriendlyPath = friendlyPath,
                                FileName = System.IO.Path.GetFileName(friendlyPath),
                                TargetIP = entry.ReplacementStrings[5],
                                TargetPort = entry.ReplacementStrings[6],
                                Protocol = FirewallHelper.getProtocolAsString(int.Parse(entry.ReplacementStrings[7])),
                                Reason = FirewallHelper.getEventInstanceIdAsString(entry.InstanceId),
                                Reason_Info = entry.Message
                            };

                            if (isAppending)
                            {
                                _logEntries.Insert(MaxEventsToLoad - (cpt + 1), le);
                            }
                            else
                            {
                                _logEntries.Add(le);
                            }

                            if (cpt == 0)
                            {
                                // We've loaded the maximum number of entries.
                                break;
                            }
                        }
                    }

                    // Trim the list
                    while (_logEntries.Count > MaxEventsToLoad)
                    {
                        _logEntries.RemoveAt(_logEntries.Count - 1);
                    }

                    // Set the cut-off point for the next time this function gets called.
                    lastDate = lastDateNew;
                    lastIndex = indexNew;
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to load the event log", e);
            }
        }

        private void btnLocate_Click(object sender, RoutedEventArgs e)
        {
            if (gridLog.SelectedItem == null)
            {
                return;
            }
            Process.Start("explorer.exe", "/select," + ((LogEntryViewModel)gridLog.SelectedItem).FriendlyPath);
        }

        private void btnEventLogVwr_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("eventvwr.msc");
        }

        private void btnRestartAdmin_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).RestartAsAdmin();
        }

        private void GridLog_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //System.Console.WriteLine($"Grid SelectionChanged: {sender}, {e.Source}, {e.Handled}, {e.OriginalSource}, {e}");
            if (gridLog.SelectedItem == null) {
                btnLocate.IsEnabled = false;
            } else {
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
            showMatchingRuleAndDetails((DataGrid)e.Source, (DataGridCell)e.OriginalSource);  // case when selection changed

        }
        private void GridLog_GotFocus(object sender, RoutedEventArgs e)
        {
            //System.Console.WriteLine($"Cell GotFocus: {sender}, {e.Source}, {e.Handled}, {e.OriginalSource}, {e}");
            showMatchingRuleAndDetails((DataGrid)e.Source, (DataGridCell)e.OriginalSource);  // case when row already selected and cell got focus
        }            

        private void showMatchingRuleAndDetails(DataGrid grid, DataGridCell cell)
        {
            LogEntryViewModel selectedEntry = (LogEntryViewModel)grid.SelectedItem;
            if (selectedEntry != null && Reason.Equals(cell.Column) && cell.IsFocused && cell.IsSelected)
            {
                LogHelper.Debug("\nGetMatchingRulesForEvent:");
                IEnumerable<FirewallHelper.Rule> rules = FirewallHelper.GetMatchingRulesForEvent(int.Parse(selectedEntry.Pid), selectedEntry.Path, selectedEntry.TargetIP, selectedEntry.TargetPort, blockOnly: true, outgoingOnly: false);
                string reasonDetails = "\n\nMatching Rules:";
                foreach (FirewallHelper.Rule rule in rules.Take(10))
                {
                    reasonDetails += $"\n'{rule.Name}' | {rule.ActionStr} | {rule.DirectionStr} | {rule.AppPkgId} | profile={rule.ProfilesStr} | svc={rule.ServiceName} | {System.IO.Path.GetFileName(rule.ApplicationName)}";
                }
                if (rules.Count() > 10)
                {
                    reasonDetails += "\n...more...";
                }
                else if (rules.Count() == 0)
                {
                    reasonDetails = "\n... no matching rules found ...";
                }
                showToolTip((Control)cell, selectedEntry.Reason_Info + reasonDetails);
            }
            else
            {
                closeToolTip();
            }
        }

        private static ToolTip toolTipInstance = new ToolTip  {
                Content = "",
                PlacementTarget = null,
                StaysOpen = true,
                IsOpen = false
            };

        private void showToolTip(UIElement placementTarget, String text)
        {
            toolTipInstance.PlacementTarget = placementTarget;
            toolTipInstance.Content = text;
            toolTipInstance.IsOpen = true;
            placementTarget.LostFocus += PlacementTarget_LostFocus;
       }

        private void closeToolTip()
        {
            toolTipInstance.Content = "";
            toolTipInstance.IsOpen = false;
            toolTipInstance.PlacementTarget = null;
        }

        private void PlacementTarget_LostFocus(object sender, RoutedEventArgs e)
        {
            closeToolTip();
            (sender as UIElement).LostFocus -= PlacementTarget_LostFocus;
        }

        private void showToolTip(Control control)
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

    }
}
