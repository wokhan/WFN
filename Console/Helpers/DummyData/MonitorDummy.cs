using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels;
namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.DummyData
{
    public class MonitorDummy
    {
        private List<MonitoredConnectionViewModel> _series = new List<MonitoredConnectionViewModel>();
        public List<MonitoredConnectionViewModel> Series
        {
            get { return _series; }
            set { _series = value; }
        }

        public List<int> Xs { get { return new List<int>() { 0, 10, 20 }; } }

        public List<int> Ys { get { return new List<int>() { 0, 10, 20 }; } }

        public MonitorDummy()
        {
            var s1 = new MonitoredConnectionViewModel()
            {
                Brush = new SolidColorBrush(Colors.Blue),
                Name = "Process #1"
            };
            s1.PointsOut.Add(new Point(0, 10));
            s1.PointsOut.Add(new Point(10, 20));
            s1.PointsOut.Add(new Point(20, 12));
            s1.PointsOut.Add(new Point(30, 15));
            s1.PointsOut.Add(new Point(40, 10));
            s1.PointsOut.Add(new Point(50, 90));
            s1.PointsOut.Add(new Point(60, 12));

            s1.PointsIn.Add(new Point(0, 12));
            s1.PointsIn.Add(new Point(10, 90));
            s1.PointsIn.Add(new Point(20, 20));
            s1.PointsIn.Add(new Point(30, 20));
            s1.PointsIn.Add(new Point(40, 12));
            s1.PointsIn.Add(new Point(50, 50));
            s1.PointsIn.Add(new Point(60, 10));
            
            s1.ConnectionsCount = 2;
            
            s1.IsSelected = true;

            Series.Add(s1);

        }
    }
}
