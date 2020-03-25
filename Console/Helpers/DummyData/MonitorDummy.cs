using System.Collections.ObjectModel;
using Wokhan.WindowsFirewallNotifier.Console.UI.Controls;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.DummyData
{
    public class MonitorDummy
    {
        public ObservableCollection<LineChart.Series> DataSeries { get; set; } = new ObservableCollection<LineChart.Series>();

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
