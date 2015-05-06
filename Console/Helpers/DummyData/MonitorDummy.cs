using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.DummyData
{
    public class MonitorDummy
    {
        public class SeriesClass
        {
            public string Name { get; set; }

            private ObservableCollection<Point> _points = new ObservableCollection<Point>();
            public ObservableCollection<Point> Points { get { return _points; } }

            private ObservableCollection<Point> _pointsIn = new ObservableCollection<Point>();
            public ObservableCollection<Point> PointsIn { get { return _points; } }
            public SolidColorBrush Brush { get; set; }

        }

        public IEnumerable<int> Xs { get { return Enumerable.Range(0, (int)Series.Max(s => s.Points.Max(p => p.X))); } }

        public IEnumerable<int> Ys { get { return Enumerable.Range(0, (int)Series.Max(s => s.Points.Max(p => p.Y))); } }

        private ObservableCollection<SeriesClass> _series = new ObservableCollection<SeriesClass>();
        public ObservableCollection<SeriesClass> Series
        {
            get { return _series; }
            set { _series = value; }
        }

        public MonitorDummy()
        {
            var s1 = new SeriesClass()
            {
                Brush = new SolidColorBrush(Colors.Blue),
                Name = "Process #1"
            };
            s1.Points.Add(new Point(0, 10));
            s1.Points.Add(new Point(10, 20));
            s1.Points.Add(new Point(20, 12));
            s1.Points.Add(new Point(30, 150));
            s1.Points.Add(new Point(40, 10));
            s1.Points.Add(new Point(50, 901));
            s1.Points.Add(new Point(60, 120));

            s1.PointsIn.Add(new Point(0, 10));
            s1.PointsIn.Add(new Point(10, 90));
            s1.PointsIn.Add(new Point(20, 10));
            s1.PointsIn.Add(new Point(30, 50));
            s1.PointsIn.Add(new Point(40, 120));
            s1.PointsIn.Add(new Point(50, 50));
            s1.PointsIn.Add(new Point(60, 120));

            Series.Add(s1);

        }
    }
}
