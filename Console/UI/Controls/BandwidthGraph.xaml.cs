using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Wokhan.Core.Core;
using Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Controls
{

    /// <summary>
    /// Logique d'interaction pour BandwidthGraph.xaml
    /// </summary>
    public partial class BandwidthGraph : UserControl
    {
        
        public ObservableCollection<Connection> Connections { get => (ObservableCollection<Connection>)GetValue(ConnectionsProperty); set => SetValue(ConnectionsProperty, value); }
        public static readonly DependencyProperty ConnectionsProperty = DependencyProperty.Register(nameof(Connections), typeof(ObservableCollection<Connection>), typeof(BandwidthGraph));
        
        public PlotModel Model { get; private set; }
        
        private const int MAX_DURATION_SEC = 10;
        
        private DateTime datetime = DateTime.Now;
        private double Start => DateTimeAxis.ToDouble(datetime.AddSeconds(-MAX_DURATION_SEC));
        private double End => DateTimeAxis.ToDouble(datetime.AddSeconds(2));

        private int Interval = 1;

        public BandwidthGraph()
        {
            Model = new PlotModel()
            {
                Axes = {
                    new DateTimeAxis() { Position = AxisPosition.Bottom, StringFormat = "HH:mm:ss", Minimum = Start, Maximum = End },
                    new LinearAxis() { Position = AxisPosition.Left, Minimum = 0, LabelFormatter = (y) => UnitFormatter.FormatBytes(y, "ps") }
                },
                IsLegendVisible = false
            };

            InitializeComponent();
        }

        public void UpdateGraph()
        {
            datetime = DateTime.Now;

            Model.DefaultXAxis.Minimum = Start;
            Model.DefaultXAxis.Maximum = End;

            var localConnections = Dispatcher.Invoke(() => Connections.GroupBy(connection => connection.GroupKey).ToList());
            foreach (var connectionGroup in localConnections)
            {
                var seriesInTitle = $"{connectionGroup.Key}#In";
                var seriesOutTitle = $"{connectionGroup.Key}#Out";
                var seriesIn = (ObservableCollection<DataPoint>)((LineSeries)Model.Series.FirstOrDefault(s => s.Title == seriesInTitle))?.ItemsSource;
                var seriesOut = (ObservableCollection<DataPoint>)((LineSeries)Model.Series.FirstOrDefault(s => s.Title == seriesOutTitle))?.ItemsSource;

                if (seriesIn is null)
                {
                    var colorbrush = Dispatcher.Invoke(() => (connectionGroup.First().Brush as SolidColorBrush).Color);
                    var color = OxyColor.FromArgb(colorbrush.A, colorbrush.R, colorbrush.G, colorbrush.B);
                    Model.Series.Add(new LineSeries() { Title = seriesInTitle, Color = color, ItemsSource = seriesIn = new ObservableCollection<DataPoint>(), InterpolationAlgorithm = InterpolationAlgorithms.CatmullRomSpline });
                    Model.Series.Add(new LineSeries() { Title = seriesOutTitle, Color = color, ItemsSource = seriesOut = new ObservableCollection<DataPoint>(), InterpolationAlgorithm = InterpolationAlgorithms.CatmullRomSpline });
                }

                var lastIn = connectionGroup.Sum(connection => connection.InboundBandwidth);
                double? prevIn = null;
                if (seriesIn.Count == 0 || (prevIn = seriesIn.Last().Y) != lastIn)
                {
                    if (prevIn.HasValue) seriesIn.Add(DateTimeAxis.CreateDataPoint(datetime.AddSeconds(-Interval), prevIn.Value));
                    seriesIn.Add(DateTimeAxis.CreateDataPoint(datetime, connectionGroup.Sum(connection => connection.InboundBandwidth)));
                }

                double? prevOut = null;
                var lastOut = connectionGroup.Sum(connection => connection.InboundBandwidth);
                if (seriesOut.Count == 0 || (prevOut = seriesIn.Last().Y) != lastOut)
                {
                    if (prevOut.HasValue) seriesIn.Add(DateTimeAxis.CreateDataPoint(datetime.AddSeconds(-Interval), prevOut.Value));
                    seriesOut.Add(DateTimeAxis.CreateDataPoint(datetime, connectionGroup.Sum(connection => connection.OutboundBandwidth)));
                }
            }

            Model.InvalidatePlot(true);
        }
    }
}
