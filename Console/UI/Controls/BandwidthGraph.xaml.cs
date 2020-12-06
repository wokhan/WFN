using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using Wokhan.Core.Core;
using Wokhan.WindowsFirewallNotifier.Console.ViewModels;

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
        private ConcurrentDictionary<string, ObservableCollection<DataPoint>> allSeriesIn = new ConcurrentDictionary<string, ObservableCollection<DataPoint>>();
        private ConcurrentDictionary<string, ObservableCollection<DataPoint>> allSeriesOut = new ConcurrentDictionary<string, ObservableCollection<DataPoint>>();

        private double Start => DateTimeAxis.ToDouble(datetime.AddSeconds(-MAX_DURATION_SEC));
        private double End => DateTimeAxis.ToDouble(datetime);

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
                ObservableCollection<DataPoint> seriesInValues;
                ObservableCollection<DataPoint> seriesOutValues;

                if (!allSeriesIn.TryGetValue(connectionGroup.Key, out seriesInValues))
                {
                    var colorbrush = connectionGroup.First().Color;
                    var color = OxyColor.FromArgb(colorbrush.A, colorbrush.R, colorbrush.G, colorbrush.B);
                    Model.Series.Add(new LineSeries() { Title = $"{connectionGroup.Key}#In", Color = color, ItemsSource = seriesInValues = new ObservableCollection<DataPoint>() });
                    Model.Series.Add(new LineSeries() { Title = $"{connectionGroup.Key}#Out", Color = color, LineStyle = LineStyle.Dash, ItemsSource = seriesOutValues = new ObservableCollection<DataPoint>() });

                    allSeriesIn.TryAdd(connectionGroup.Key, seriesInValues);
                    allSeriesOut.TryAdd(connectionGroup.Key, seriesOutValues);
                }
                else
                {
                    seriesOutValues = allSeriesOut[connectionGroup.Key];
                }
                
                var lastIn = connectionGroup.Sum(connection => (long)connection.InboundBandwidth);
                seriesInValues.Add(DateTimeAxis.CreateDataPoint(datetime, lastIn));
                if (seriesInValues.Count > 3 && seriesInValues[^2].Y == lastIn && seriesInValues[^3].Y == lastIn)
                {
                    seriesInValues.RemoveAt(seriesInValues.Count - 2);
                }

                var lastOut = connectionGroup.Sum(connection => (long)connection.OutboundBandwidth);
                seriesOutValues.Add(DateTimeAxis.CreateDataPoint(datetime, lastOut));
                if (seriesOutValues.Count > 3 && seriesOutValues[^2].Y == lastOut && seriesOutValues[^3].Y == lastOut)
                {
                    seriesOutValues.RemoveAt(seriesOutValues.Count - 2);
                }
            }

            Model.InvalidatePlot(true);
        }
    }
}
