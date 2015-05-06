using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Wokhan.WindowsFirewallNotifier.Common.Helpers.IPHelpers;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Interaction logic for Monitor.xaml
    /// </summary>
    public partial class Monitor : Page
    {
        public List<Color> ColorsDic = typeof(Colors).GetProperties().Select(m => m.GetValue(null)).Cast<Color>().Where(c => c.A > 150 && c.R < 150 && c.G < 150 && c.B < 150).ToList();

        public class SeriesClass : INotifyPropertyChanged
        {
            public string Name { get; set; }
            private ObservableCollection<Point> _points = new ObservableCollection<Point>();
            public ObservableCollection<Point> Points { get { return _points; } }

            private ObservableCollection<Point> _pointsIn = new ObservableCollection<Point>();
            public ObservableCollection<Point> PointsIn { get { return _pointsIn; } }
            public SolidColorBrush Brush { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;

            public SeriesClass()
            {
                _points.CollectionChanged += (o, e) => NotifyPropertyChanged("Points");
                _pointsIn.CollectionChanged += (o, e) => NotifyPropertyChanged("PointsIn");
            }

            void _points_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            {
                NotifyPropertyChanged("Points");
            }

            private void NotifyPropertyChanged(string caller)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(caller));
                }
            }
        }

        public IEnumerable<int> Xs { get { return Series.Any() ? Enumerable.Range(0, (int)Series.Max(s => s.Points.Max(p => p.X))) : new[] { 0, 10 }; } }

        public IEnumerable<int> Ys { get { return Series.Any() ? Enumerable.Range(0, (int)Series.Max(s => s.Points.Max(p => p.Y))) : new[] { 0, 100 }; } }

        public List<double> Intervals { get { return new List<double> { 0.05, 0.1, 0.5, 1, 5, 10 }; } }

        private DispatcherTimer timer = new DispatcherTimer();

        private double _interval = 0.05;
        public double Interval
        {
            get { return _interval; }
            set { _interval = value; timer.Interval = TimeSpan.FromSeconds(value); }
        }

        private ObservableCollection<SeriesClass> _series = new ObservableCollection<SeriesClass>();
        public ObservableCollection<SeriesClass> Series
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

            var conn = TCPHelper.GetAllTCPConnections().Where(co => co.OwnerModule != null);

            for (int i = Series.Count - 1; i > 0; i--)
            {
                var s = Series[i];
                if (!conn.Any(c => c.OwnerModule.ModuleName == s.Name))
                {
                    Series.RemoveAt(i);
                }
            }
            
            foreach (var c in conn)
            {
                var existing = Series.FirstOrDefault(s => s.Name == c.OwnerModule.ModuleName);
                if (existing == null)
                {
                    existing = new SeriesClass() { Name = c.OwnerModule.ModuleName, Brush = new SolidColorBrush(ColorsDic[Series.Count]) };
                    Series.Add(existing);
                }

                var r = TCPHelper.GetTCPStatistics(c);

                existing.Points.Add(new Point(GetX(currentX), GetY(r.DataBytesOut)));
                existing.PointsIn.Add(new Point(GetX(currentX), GetY(r.DataBytesIn)));
            }
        }

        private double GetY(double value)
        {
            return chartZone.ActualHeight / 10.0 * Math.Log10(value);
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
    }
}
