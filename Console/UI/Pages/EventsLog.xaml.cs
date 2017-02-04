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
                    EventLogEntry entry;
                    bool isAppending = (_logEntries.Any());
                    DateTime lastDateLocal = DateTime.MinValue;
                    int indexLocal = 0;

                    while (i > 0 && cpt > 0)
                    {
                        entry = securityLog.Entries[i--];

                        if (lastDate != DateTime.MinValue && entry.TimeWritten <= lastDate && (entry.Index == lastIndex || lastIndex == -1))
                        {
                            break;
                        }

                        if (entry.InstanceId == 5157 && entry.EntryType == EventLogEntryType.FailureAudit)
                        {
                            cpt--;
                            var le = new LogEntryViewModel()
                            {
                                CreationTime = entry.TimeWritten,
                                Icon = IconHelper.GetIcon(entry.ReplacementStrings[1]),
                                FriendlyPath = FileHelper.GetFriendlyPath(entry.ReplacementStrings[1]),
                                Replacement5 = entry.ReplacementStrings[5],
                                Protocol = FirewallHelper.getProtocolAsString(int.Parse(entry.ReplacementStrings[7])),
                                Replacement6 = entry.ReplacementStrings[6]
                            };

                            if (isAppending)
                            {
                                _logEntries.Insert(0, le);
                            }
                            else
                            {
                                _logEntries.Add(le);
                            }
                        }

                        if (cpt >= MaxEventsToLoad - 1)
                        {
                            lastDateLocal = securityLog.Entries[i].TimeWritten;
                            indexLocal = securityLog.Entries[i].Index;
                        }
                    }

                    if (cpt == 0)
                    {
                        lastDate = DateTime.Now;
                        lastIndex = -1;
                    }
                    else
                    {
                        lastDate = lastDateLocal;
                        lastIndex = indexLocal;
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to load the event log", e);
            }
        }

        private void btnLocate_Click(object sender, RoutedEventArgs e)
        {
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
    }
}
