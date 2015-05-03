using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WindowsFirewallNotifier;
using WFNConsole.Helpers;
using System.Windows.Threading;

namespace WFNConsole.UI.Pages
{
    /// <summary>
    /// Interaction logic for EventLog.xaml
    /// </summary>
    public partial class EventsLog : Page
    {
        public class LogEntry
        {
            public DateTime CreationTime { get; set; }
            public ImageSource Icon { get; set; }
            public string FriendlyPath { get; set; }
            public string Replacement5 { get; set; }
            public string Protocol { get; set; }
            public string Replacement6 { get; set; }
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

            this.DataContext = this;
        }

        void timer_Tick(object sender, EventArgs e)
        {
           initEventLog();
        }


        private ObservableCollection<LogEntry> _logEntries = new ObservableCollection<LogEntry>();
        public ObservableCollection<LogEntry> LogEntries { get { return _logEntries; } }

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
                            var le = new LogEntry()
                                                    {
                                                        CreationTime = entry.TimeWritten,
                                                        Icon = ProcessHelper.GetIcon(CommonHelper.GetFriendlyPath(entry.ReplacementStrings[1])).AsImageSource(),
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


        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            timer.Start();
            btnStart.IsEnabled = false;
            btnStop.IsEnabled = true;
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
        }

        private void btnLocate_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", "/select," + ((LogEntry)gridLog.SelectedItem).FriendlyPath);
        }

        private void btnEventLogVwr_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("eventvwr.msc");
        }

    }
}
