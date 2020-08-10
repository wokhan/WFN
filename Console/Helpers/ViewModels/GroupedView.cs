using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Geared;
using LiveCharts.Wpf;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels
{
    public class GroupedView : GroupedViewBase
    {

        private double _lastin;
        public double LastIn
        {
            get { return _lastin; }
            set { _lastin = value; NotifyPropertyChanged(nameof(LastIn)); }
        }

        private double _lastout;
        public double LastOut
        {
            get { return _lastout; }
            set { _lastout = value; NotifyPropertyChanged(nameof(LastOut)); }
        }

        public GearedValues<DateTimePoint> SeriesIn { get; } = new GearedValues<DateTimePoint>();
        public GearedValues<DateTimePoint> SeriesOut { get; } = new GearedValues<DateTimePoint>();

    }
}
