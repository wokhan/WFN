using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Controls
{
    /// <summary>
    /// Interaction logic for LineGraph.xaml
    /// </summary>
    public partial class LineChart : UserControl, INotifyPropertyChanged
    {
        public class Series : INotifyPropertyChanged
        {
            //[Bindable(true, BindingDirection.OneWay)]
            public Brush Brush { get; set; }

            //[Bindable(true, BindingDirection.OneWay)]
            public string Name { get; set; }

            /*[Bindable(true, BindingDirection.OneWay)]
            public double MaxX { get; set; }

            [Bindable(true, BindingDirection.OneWay)]
            public double MaxY { get; set; }

            [Bindable(true, BindingDirection.OneWay)]
            public double StartX { get; set; }

            [Bindable(true, BindingDirection.OneWay)]
            public double XScaleFactor { get; set; }
            */
            private bool _isSelected;
            //[Bindable(true, BindingDirection.TwoWay)]
            public bool IsSelected
            {
                get { return _isSelected; }
                set
                {
                    _isSelected = value;
                    if (SelectionChanged != null)
                    {
                        SelectionChanged(this, null);
                    }
                    NotifyPropertyChanged(nameof(IsSelected));
                }
            }

            public Func<IEnumerable<Point>, PointCollection> PointTransformer;

            private ObservableCollection<Point> _points = new ObservableCollection<Point>();
            public ObservableCollection<Point> Points { get { return _points; } }

            public PointCollection PointsCollection { get { return PointTransformer(_points); } }

            public event NotifyCollectionChangedEventHandler PointsCollectionChanged;

            public event SelectionChangedEventHandler SelectionChanged;

            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(string caller)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(caller));
                }
            }

            public Series()
            {
                _points.CollectionChanged += _points_CollectionChanged;
            }

            private void _points_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (PointsCollectionChanged != null)
                {
                    PointsCollectionChanged(sender, e);
                }

                NotifyPropertyChanged(nameof(PointsCollection));
            }
        }

        public enum AxeTypes
        {
            DateTime,
            Timestamp,
            Numeric,
            Other
        }

        public AxeTypes XAxeType { get; set; }

        public string XAxeLabelFormat { get; set; }

        [Bindable(true, BindingDirection.OneWay)]
        public ObservableCollection<Series> DataSeries
        {
            get { return (ObservableCollection<Series>)GetValue(DataSeriesProperty); }
            set { SetValue(DataSeriesProperty, value); }
        }

        public static readonly DependencyProperty DataSeriesProperty = DependencyProperty.Register("DataSeries", typeof(ObservableCollection<Series>), typeof(LineChart));

        public double XTranslate { get { return 0; } }// -_xs.ElementAtOrDefault(DisplayedUnits) : 0; } }

        public double XScaleFactor { get { return _xs.Any() && chartZone.ActualWidth != 0 ? (chartZone.ActualWidth / (_xs.Last() - _xs[0] - XTranslate)) : 1; } }
        public double YScaleFactor { get { return _ys.Any() && chartZone.ActualHeight != 0 ? (chartZone.ActualHeight / (_ys.Last() - _ys[0])) : 1; } }

        public double XMaxStartDelta { get; set; }
        public double YMaxStartDelta { get; set; }

        public Func<double, object> XFuncConverter = (x) => x;
        public Func<double, object> YFuncConverter = (y) => y;

        private int _numberToKeep = 5;
        public int NumberToKeep
        {
            get { return _numberToKeep; }
            set { _numberToKeep = value; }
        }

        private int _xTicksFrequency = 10;
        public int XTicksFrequency
        {
            get { return _xTicksFrequency; }
            set { _xTicksFrequency = value; }
        }

        private int _yTicksFrequency = 10;
        public int YTicksFrequency
        {
            get { return _yTicksFrequency; }
            set { _yTicksFrequency = value; }
        }
        //public Func<long, long> XRoundingFormula { get; set; }
        //public Func<long, long> YRoundingFormula { get; set; }

        //public bool AllowScrolling { get; set; }

        private List<double> _xs = new List<double>();
        public object Xs { get { return _xs.Select(x => new { RealValue = XFuncConverter(x), ScaledValue = (x - (_xs.Count > 1 ? _xs[0] : 0) + XTranslate) * XScaleFactor }); } }

        private List<double> _ys = new List<double>();
        public object Ys { get { return _ys.Select(y => new { RealValue = YFuncConverter(y), ScaledValue = (y - (_ys.Count > 1 ? _ys[0] : 0)) * YScaleFactor }); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public static List<Color> ColorsDic = typeof(Colors).GetProperties().Select(m => m.GetValue(null)).Cast<Color>().Where(c => c.A > 200 && c.R < 150 && c.G < 150 && c.B < 150).ToList();

        public LineChart()
        {
            InitializeComponent();

            this.Loaded += LineChart_Loaded;
        }

        private void LineChart_Loaded(object sender, RoutedEventArgs e)
        {
            DataSeries.CollectionChanged += DataSeries_CollectionChanged;
        }

        private void DataSeries_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (var series in e.NewItems.Cast<Series>())
            {
                series.PointTransformer = PointTransformer;
                series.PointsCollectionChanged += Points_CollectionChanged;
            }
        }

        private void Points_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var points = e.NewItems.Cast<Point>();
            var source = (ObservableCollection<Point>)sender;

            //_xs = _xs.SkipWhile(x => x < source.DefaultIfEmpty(new Point(0, 0)).ElementAtOrDefault(source.Count - _numberToKeep).X).ToList();

            if (SetAutomaticScale(points.Min(p => p.X), points.Max(p => p.X), XMaxStartDelta, XTicksFrequency, _xs))
            {
                NotifyPropertyChanged(nameof(Xs));
            }

            // Have to use the whole points source instead of the added ones, probably because of some latency preventing
            // things to occur when needed.
            if (SetAutomaticScale(source.Min(p => p.Y), source.Max(p => p.Y), YMaxStartDelta, YTicksFrequency, _ys))
            {
                NotifyPropertyChanged(nameof(Ys));
            }
        }

        private static bool SetAutomaticScale(double min, double max, double defaultMaxDelta, int occurences, List<double> scale)
        {
            double newMin;
            double newMax;
            double currentMax = scale.LastOrDefault();
            double currentMin = scale.FirstOrDefault();

            if (scale.Count == 0)
            {
                newMin = min;
                newMax = Math.Max(max, min + defaultMaxDelta);
            }
            else
            {
                newMax = Math.Max(max, currentMax);
                newMin = Math.Min(min, currentMin);
            }

            if (scale.Count == 0 || newMax > currentMax || newMin < currentMin)
            {
                var blockSize = (newMax - newMin) / occurences;
                scale.Clear();
                scale.AddRange(Enumerable.Range(0, occurences + 1)
                                         .Select(i => Math.Ceiling(newMin + i * blockSize))
                                         .Distinct());

                return true;
            }
            else
            {
                return false;
            }
        }

        private PointCollection PointTransformer(IEnumerable<Point> arg)
        {
            return new PointCollection(arg.SkipWhile(p => p.X < _xs[0]).Select(p => new Point((p.X - _xs[0] + XTranslate) * XScaleFactor, p.Y * YScaleFactor)));
        }

        private void NotifyPropertyChanged(string caller)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
            }
        }

        private void line_MouseEnter(object sender, MouseEventArgs e)
        {
            Polyline line = (Polyline)sender;
            ((Series)line.Tag).IsSelected = true;
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }
    }
}
