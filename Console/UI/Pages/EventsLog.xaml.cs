using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        
        public bool IsTrackingEnabled
        {
            get { return timer.IsEnabled; }
            set { timer.IsEnabled = value; }
        }

        private DispatcherTimer timer = new DispatcherTimer();

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

            //   initEventLog();

            timer.Tick += timer_Tick;
            timer.Interval = TimeSpan.FromSeconds(Interval);
            timer.Start();
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
                    int cpt = 500;
                    EventLogEntry entry;
                    string friendlyPath;
                    bool isAppending = (_logEntries.Any());
                    DateTime lastDateLocal = DateTime.MinValue;
                    int indexLocal = 0;

                    while (i > 0 && cpt > 0)
                    {
                        entry = securityLog.Entries[i--];

                        if (lastDate != DateTime.MinValue && entry.TimeWritten <= lastDate && entry.Index == lastIndex)
                        {
                            break;
                        }

                        if (entry.InstanceId == 5157 && entry.EntryType == EventLogEntryType.FailureAudit)
                        {
                            cpt--;
                            friendlyPath = CommonHelper.GetFriendlyPath(entry.ReplacementStrings[1]);
                            var le = new LogEntryViewModel()
                                                    {
                                                        CreationTime = entry.TimeWritten,
                                                        Icon = ProcessHelper.GetCachedIcon(CommonHelper.GetFriendlyPath(entry.ReplacementStrings[1])),
                                                        FriendlyPath = CommonHelper.GetFriendlyPath(entry.ReplacementStrings[1]),
                                                        Replacement5 = entry.ReplacementStrings[5],
                                                        Protocol = getProtocol(entry.ReplacementStrings[7]),
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

                        if (cpt >= 499)
                        {
                            lastDateLocal = securityLog.Entries[i].TimeWritten;
                            indexLocal = securityLog.Entries[i].Index;
                        }
                    }

                    lastDate = lastDateLocal;
                    lastIndex = indexLocal;
                }

            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to load the event log", e);
            }
        }

        private string getProtocol(string p)
        {
            try
            {
                switch (int.Parse(p))
                {
                    case 6:
                        return "TCP";

                    case 17:
                        return "UDP";

                    default:
                        return "Other";
                }
            }
            catch
            {
                return "Unknown";
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

    }
}
