using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels;
using Wokhan.WindowsFirewallNotifier.Console.UI.Controls;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.DummyData
{
    public class MonitorDummy
    {
        private ObservableCollection<LineChart.Series> _series = new ObservableCollection<LineChart.Series>();
        public ObservableCollection<LineChart.Series> DataSeries
        {
            get { return _series; }
            set { _series = value; }
        }

        public dynamic[] Xs = new[] { new { RealValue = 0, ScaledValue = 0 },
                new { RealValue = 10, ScaledValue = 50 },
                new { RealValue = 20, ScaledValue = 100 },
                new { RealValue = 30, ScaledValue = 150 }};
        public dynamic[] Ys = new[] { new { RealValue = 0, ScaledValue = 0 },
                new { RealValue = 10, ScaledValue = 50 },
                new { RealValue = 20, ScaledValue = 100 },
                new { RealValue = 30, ScaledValue = 150 }};

        public MonitorDummy()
        {
        }
    }
}
