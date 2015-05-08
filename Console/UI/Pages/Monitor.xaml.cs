using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Wokhan.WindowsFirewallNotifier.Common.Helpers.IPHelpers;
using Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Interaction logic for Monitor.xaml
    /// </summary>
    public partial class Monitor : Page, INotifyPropertyChanged
    {
        public List<Color> ColorsDic = typeof(Colors).GetProperties().Select(m => m.GetValue(null)).Cast<Color>().Where(c => c.A > 150 && c.R < 150 && c.G < 150 && c.B < 150).ToList();

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string caller)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
            }
        }

        private double lastMax = 0;

        private double transformScaleY = -1;

        public double TransformScaleY
        {
            get { return transformScaleY; }
            set { transformScaleY = value; NotifyPropertyChanged("TransformScaleY"); }
        }

        public bool IsTrackingEnabled
        {
            get { return timer.IsEnabled; }
            set { timer.IsEnabled = value; }
        }


        public List<int> Xs { get { return new List<int>() { 0, 10, 20 }; } }

        public List<int> Ys { get { return new List<int>() { 0, 10, 20 }; } }


        public List<double> Intervals { get { return new List<double> { 0.05, 0.1, 0.5, 1, 5, 10 }; } }

        private DispatcherTimer timer = new DispatcherTimer();

        private double _interval = 0.5;
        public double Interval
        {
            get { return _interval; }
            set { _interval = value; timer.Interval = TimeSpan.FromSeconds(value); }
        }

        private ObservableCollection<MonitoredConnectionViewModel> _series = new ObservableCollection<MonitoredConnectionViewModel>();
        public ObservableCollection<MonitoredConnectionViewModel> Series
        {
            get { return _series; }
            set { _series = value; }
        }

        public Monitor()
        {
            InitializeComponent();

            timer.Interval = TimeSpan.FromSeconds(Interval);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        double currentX = -1;

        private void timer_Tick(object sender, EventArgs e)
        {
            currentX += 1;

            // Boxing + unboxing operation has a cost, should be avoided... Will have a look at that later.
            var tcpc = TCPHelper.GetAllTCPConnections()
                                .Where(co => co.RemoteAddress != "0.0.0.0" && co.OwnerModule != null)
                                .Select(c => new { c.OwnerModule.ModuleName, Obj = (object)c });

            if (Socket.OSSupportsIPv6)
            {
                tcpc = tcpc.Concat(TCP6Helper.GetAllTCP6Connections()
                                             .Where(co => co.RemoteAddress != "::" &&  co.OwnerModule != null)
                                             .Select(c => new { c.OwnerModule.ModuleName, Obj = (object)c }));
            }

             var conn = tcpc.GroupBy(c => c.ModuleName);
            //var conn6 = TCP6Helper.GetAllTCP6Connections().Where(co => co.OwnerModule != null).GroupBy(c => c.OwnerModule.ModuleName);
            
            for (int i = Series.Count - 1; i > 0; i--)
            {
                var s = Series[i];
                if (!conn.Any(c => c.Key == s.Name))
                {
                    s.IsDead = true;
                    //Series.RemoveAt(i);
                }
            }
            
            foreach (var c in conn)
            {
                var existing = Series.FirstOrDefault(s => s.Name == c.Key);
                if (existing == null)
                {
                    existing = new MonitoredConnectionViewModel() { Name = c.Key, Brush = new SolidColorBrush(ColorsDic[Series.Count]) };
                    Series.Add(existing);
                }

                double sumIn = 0.0;
                double sumOut = 0.0;
                int cnt = 0;
                foreach (var realconn in c)
                {
                    cnt++;
                    try
                    {
                        var r = realconn.Obj is TCPHelper.MIB_TCPROW_OWNER_MODULE ? TCPHelper.GetTCPBandwidth((TCPHelper.MIB_TCPROW_OWNER_MODULE)realconn.Obj) : TCP6Helper.GetTCPBandwidth((TCP6Helper.MIB_TCP6ROW_OWNER_MODULE)realconn.Obj);
                        sumIn += r.InboundBandwidth;
                        sumOut += r.OutboundBandwidth;
                    }
                    catch { }
                }

                existing.ConnectionsCount = cnt;

                existing.PointsOut.Add(new Point(GetX(currentX), GetY(sumOut)));
                existing.PointsIn.Add(new Point(GetX(currentX), GetY(sumIn)));
            }

            //foreach (var c in conn6)
            //{
            //    var existing = Series.FirstOrDefault(s => s.Name == c.Key);
            //    if (existing == null)
            //    {
            //        existing = new SeriesClass() { Name = c.Key, Brush = new SolidColorBrush(ColorsDic[Series.Count]) };
            //        Series.Add(existing);
            //    }

            //    double sumIn = 0.0;
            //    double sumOut = 0.0;
            //    int cnt = 0;
            //    foreach (var realconn in c)
            //    {
            //        cnt++;

            //        var r = TCP6Helper.GetTCPStatistics(realconn);
            //        sumIn += r.DataBytesIn;
            //        sumOut += r.DataBytesOut;
            //    }

            //    existing.PointsOut.Add(new Point(GetX(currentX), GetY(sumOut)));
            //    existing.PointsIn.Add(new Point(GetX(currentX), GetY(sumIn)));
            //}
        }

        private double lastScale= 0;
        private double GetY(double value)
        {
            value = (chartZone.ActualHeight / Math.Log10(300000000) * Math.Log10(value));
            if (value > lastMax)
            {
                lastMax = value * 1.5;
                lastScale = value / lastMax;
                TransformScaleY = -lastScale;
            }
            return value;
        }

        private double GetX(double value)
        {
            return value * 2;
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
            {
                var offset = e.NewSize.Width - e.PreviousSize.Width;
                scroller.ScrollToHorizontalOffset(scroller.HorizontalOffset + offset);
            }
        }

        private void poly1_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Polyline line = (Polyline)sender;
            ((MonitoredConnectionViewModel)line.Tag).IsSelected = true;
        }
    }
}
