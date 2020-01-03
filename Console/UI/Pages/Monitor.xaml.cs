using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Common.Helpers.IPHelpers;
using Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels;
using Wokhan.WindowsFirewallNotifier.Console.UI.Controls;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Interaction logic for Monitor.xaml
    /// </summary>
    public partial class Monitor : Page, INotifyPropertyChanged
    {
        private const double GroupTimeoutRemove = 1000.0; //milliseconds

        public bool IsTrackingEnabled
        {
            get { return timer.IsEnabled; }
            set { timer.IsEnabled = value; NotifyPropertyChanged(nameof(IsTrackingEnabled)); }
        }

        private bool _isSingleMode;
        public bool IsSingleMode
        {
            get { return _isSingleMode; }
            set { _isSingleMode = value; _groupedConnections.Clear(); NotifyPropertyChanged(nameof(IsSingleMode)); }
        }

        public List<double> Intervals { get { return new List<double> { 0.2, 0.5, 1, 5, 10 }; } }

        private DispatcherTimer timer = new DispatcherTimer() { IsEnabled = true };

        private double _interval = 1;
        public double Interval
        {
            get { return _interval; }
            set { _interval = value; timer.Interval = TimeSpan.FromSeconds(value); }
        }

        //private ObservableCollection<MonitoredConnection> _connectionViews = new ObservableCollection<MonitoredConnection>();
        //public ObservableCollection<MonitoredConnection> ConnectionViews
        //{
        //    get { return _connectionViews; }
        //    set { _connectionViews = value; }
        //}

        private ObservableCollection<LineChart.Series> _series = new ObservableCollection<LineChart.Series>();
        public ObservableCollection<LineChart.Series> Series { get { return _series; } }

        public Monitor()
        {
            InitializeComponent();
            timer.Interval = TimeSpan.FromSeconds(Interval);
            timer.Tick += timer_Tick;

            chart.XMaxStartDelta = 60 * TimeSpan.TicksPerSecond;
            chart.XFuncConverter = (x) => new DateTime((long)x).ToString(DateTimeFormatInfo.CurrentInfo.LongTimePattern);
            chart.YFuncConverter = (y) => CommonHelper.FormatBytes(y, "ps");

            this.Loaded += Monitor_Loaded;
            this.Unloaded += Monitor_Unloaded;
        }

        private void Monitor_Unloaded(object sender, RoutedEventArgs e)
        {
            timer.Stop();
        }
    
        private void NotifyPropertyChanged(string caller)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
            }
        }

        void Monitor_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(() => timer_Tick(null, null));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<GroupedView> _groupedConnections = new ObservableCollection<GroupedView>();
        public ObservableCollection<GroupedView> GroupedConnections { get { return _groupedConnections; } }

        private void timer_Tick(object sender, EventArgs e)
        {
            var x = DateTime.Now.Ticks;

            var tcpc = IPHelper.GetAllConnections(true)
                               .Where(co => co.State == IPHelper.MIB_TCP_STATE.ESTABLISHED && !co.IsLoopback && co.OwnerModule != null)
                               //.AsParallel()
                               .Select(c => new MonitoredConnection(c))
                               .ToList();

            //) co.RemoteAddress != "::" && co.RemoteAddress != "0.0.0.0" 
            Func<MonitoredConnection, string> groupFn;
            if (_isSingleMode)
            {
                groupFn = (c) => c.Owner + c.LocalPort;
            }
            else
            {
                groupFn = (c) => c.ProcName;
            }

            var groups = tcpc.GroupBy(c => groupFn(c)).ToList(); // OwnerModule.ModuleName).ToList();

            int ic = 0;
            foreach (var grp in groups)
            {
                var existing = _groupedConnections.FirstOrDefault(s => s.Name == grp.Key);

                if (existing == null)
                {
                    ic = (ic + 1) % LineChart.ColorsDic.Count;

                    var br = new SolidColorBrush(LineChart.ColorsDic[ic]);
                    existing = new GroupedView { Name = grp.Key };
                    existing.Brush = br;
                    existing.Icon = grp.First().Icon;
                    existing.SeriesIn = new LineChart.Series() { Name = grp.Key + "_IN", Brush = br };
                    Series.Add(existing.SeriesIn);

                    var color = new SolidColorBrush(LineChart.ColorsDic[ic]) { Opacity = 0.6 };
                    existing.SeriesOut = new LineChart.Series() { Name = grp.Key + "_OUT", Brush = color };
                    Series.Add(existing.SeriesOut);

                    _groupedConnections.Add(existing);
                }

                bool iserror = false;
                var totalized = grp.AsParallel()
                                 .Select(realconn =>
                                 {
                                     try { return realconn.EstimateBandwidth(); }
                                     catch { iserror = true; return new TCPHelper.TCP_ESTATS_BANDWIDTH_ROD_v0 { InboundBandwidth = 0, OutboundBandwidth = 0 }; }
                                 })
                                 .Select(co => new { In = co.InboundBandwidth / 8, Out = co.OutboundBandwidth / 8 })
                                 .Aggregate((ci, co) => new { In = ci.In + co.In, Out = ci.Out + co.Out });

                existing.Count = grp.Count();
                existing.LastIn = CommonHelper.FormatBytes(totalized.In, "ps");
                existing.LastOut = CommonHelper.FormatBytes(totalized.Out, "ps");
                existing.SeriesOut.Points.Add(new Point(x, totalized.Out));
                existing.SeriesIn.Points.Add(new Point(x, totalized.In));
                
                existing.IsAccessDenied = iserror;
                existing.LastSeen = DateTime.Now;
                //foreach (var realconn in grp)
                //{
                //    cnt++;
                //    try
                //    {
                //        var r = realconn.EstimateBandwidth();
                //        sumIn += r.InboundBandwidth / 8.0;
                //        sumOut += r.OutboundBandwidth / 8.0;
                //    }
                //    catch (Exception exc)
                //    {
                //        lastError = exc.Message;
                //        accessDenied = true;
                //    }
                //}

                //existing.LastError = lastError;
                //existing.IsAccessDenied = accessDenied;
                //existing.ConnectionsCount = cnt;

            }

            var removableGr = _groupedConnections.Where(gr => DateTime.Now.Subtract(gr.LastSeen).TotalMilliseconds > GroupTimeoutRemove).ToList();
            foreach (var g in removableGr)
            {
                g.LastIn = "-";
                g.LastOut = "-";
                g.Count = 0;
                g.Brush = new SolidColorBrush(Colors.LightGray);
                _groupedConnections.Remove(g);
            }
        }

        private void btnRestartAdmin_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).RestartAsAdmin();
        }
    }
}
