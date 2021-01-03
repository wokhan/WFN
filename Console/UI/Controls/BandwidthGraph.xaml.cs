using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using Wokhan.Core.Core;
using Wokhan.WindowsFirewallNotifier.Console.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Controls
{
    public partial class BandwidthGraph : UserControl, INotifyPropertyChanged
    {

        public ObservableCollection<Connection> Connections { get => (ObservableCollection<Connection>)GetValue(ConnectionsProperty); set => SetValue(ConnectionsProperty, value); }
        public static readonly DependencyProperty ConnectionsProperty = DependencyProperty.Register(nameof(Connections), typeof(ObservableCollection<Connection>), typeof(BandwidthGraph));

        public PlotModel Model { get; private set; }
        public PlotModel MiniModel { get; private set; }

        private const int MAX_DURATION_SEC = 10;

        private bool isXPanned;

        private DateTime datetime = DateTime.Now;

        private ConcurrentDictionary<string, (ObservableCollection<DataPoint> In, ObservableCollection<DataPoint> Out)> allSeries = new ConcurrentDictionary<string, (ObservableCollection<DataPoint>, ObservableCollection<DataPoint>)>();

        private ObservableCollection<DataPoint> seriesOutTotal;
        private ObservableCollection<DataPoint> seriesInTotal;

        public event PropertyChangedEventHandler PropertyChanged;
        public double ThumbSize => (Model.DefaultXAxis.ActualMaximum - Model.DefaultXAxis.ActualMinimum) * scrollArea.Track.ActualWidth / (Model.DefaultXAxis.DataMaximum - Model.DefaultXAxis.DataMinimum);

        public double CurrentStart
        {
            get => (Model.DefaultXAxis != null ? Model.DefaultXAxis.Minimum + (Model.DefaultXAxis.Maximum - Model.DefaultXAxis.Minimum) / 2 : 0);
            set
            {
                if (isXPanned)
                {
                    var window = (Model.DefaultXAxis.Maximum - Model.DefaultXAxis.Minimum) / 2;
                    Model.DefaultXAxis.Minimum = value - window;
                    Model.DefaultXAxis.Maximum = value + window;

                    Model.InvalidatePlot(false);

                    NotifyPropertyChanged();
                }
            }
        }

        public double AbsoluteStart => Model.DefaultXAxis?.AbsoluteMinimum ?? 0;
        public double AbsoluteEnd => Model.DefaultXAxis?.AbsoluteMaximum ?? 0;

        public BandwidthGraph()
        {
            var xAxis = new DateTimeAxis() { Position = AxisPosition.Bottom, StringFormat = "HH:mm:ss", IsPanEnabled = true, IsZoomEnabled = true, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = OxyColors.LightGray };
            var yAxis = new LinearAxis() { Position = AxisPosition.Left, Minimum = 0, LabelFormatter = (y) => UnitFormatter.FormatBytes(y, "ps"), IsZoomEnabled = false, IsPanEnabled = false };

            xAxis.AxisChanged += XAxis_AxisChanged;

            Model = new PlotModel()
            {
                Axes = { xAxis, yAxis },
                IsLegendVisible = false
            };

            MiniModel = new PlotModel()
            {
                Axes = {
                    new DateTimeAxis() { IsAxisVisible = false },
                    new LinearAxis() { IsAxisVisible=false, Minimum = 0, LabelFormatter = (y) => UnitFormatter.FormatBytes(y, "ps") }
                },
                Series = {
                    new LineSeries() { Color = OxyColors.LightGreen, ItemsSource = seriesInTotal = new ObservableCollection<DataPoint>(), CanTrackerInterpolatePoints = false },
                    new LineSeries() { Color = OxyColors.OrangeRed, ItemsSource = seriesOutTotal = new ObservableCollection<DataPoint>(), CanTrackerInterpolatePoints = false }
                },
                IsLegendVisible = false
            };

            InitializeComponent();
        }

        private void XAxis_AxisChanged(object sender, AxisChangedEventArgs e)
        {
            if (e.ChangeType.HasFlag(AxisChangeTypes.Zoom))
            {
                NotifyPropertyChanged(nameof(ThumbSize));
                NotifyPropertyChanged(nameof(CurrentStart));
            }
        }

        public void UpdateGraph()
        {
            datetime = DateTime.Now;

            var localConnections = Dispatcher.Invoke(() => Connections.GroupBy(connection => connection.Path));
            long currentIn = 0;
            long currentOut = 0;
            foreach (var connectionGroup in localConnections.AsParallel())
            {
                ObservableCollection<DataPoint> seriesInValues;
                ObservableCollection<DataPoint> seriesOutValues;

                if (!allSeries.TryGetValue(connectionGroup.Key, out var seriesValues))
                {
                    var color = connectionGroup.First().Color;
                    var seriesColor = OxyColor.FromArgb(color.A, color.R, color.G, color.B);
                    Model.Series.Add(new LineSeries() { Title = $"{connectionGroup.Key}#In", Color = seriesColor, ItemsSource = seriesInValues = new ObservableCollection<DataPoint>(), CanTrackerInterpolatePoints = false });
                    Model.Series.Add(new LineSeries() { Title = $"{connectionGroup.Key}#Out", Color = seriesColor, LineStyle = LineStyle.Dash, ItemsSource = seriesOutValues = new ObservableCollection<DataPoint>(), CanTrackerInterpolatePoints = false });

                    allSeries.TryAdd(connectionGroup.Key, (seriesInValues, seriesOutValues));
                }
                else
                {
                    (seriesInValues, seriesOutValues) = seriesValues;
                }

                var lastSumIn = connectionGroup.Sum(connection => (long)connection.InboundBandwidth);
                currentIn += lastSumIn;
                AddAndMergePoints(seriesInValues, lastSumIn);

                var lastSumOut = connectionGroup.Sum(connection => (long)connection.OutboundBandwidth);
                currentOut += lastSumOut;
                AddAndMergePoints(seriesOutValues, lastSumOut);
            }

            AddAndMergePoints(seriesInTotal, currentIn);
            AddAndMergePoints(seriesOutTotal, currentOut);

            Model.DefaultXAxis.AbsoluteMinimum = Math.Max(Math.Min(Model.DefaultXAxis.Minimum, Model.DefaultXAxis.DataMinimum), DateTimeAxis.ToDouble(datetime.AddSeconds(-120)));
            Model.DefaultXAxis.AbsoluteMaximum = Model.DefaultXAxis.DataMaximum;

            // Sync both models (mini and standard) absolute minimum 
            MiniModel.DefaultXAxis.AbsoluteMinimum = Model.DefaultXAxis.AbsoluteMinimum;

            NotifyPropertyChanged(nameof(AbsoluteStart));
            NotifyPropertyChanged(nameof(AbsoluteEnd));
            NotifyPropertyChanged(nameof(ThumbSize));

            // If scrolling has not been manually forced
            if (!isXPanned)
            {
                Model.DefaultXAxis.Minimum = DateTimeAxis.ToDouble(datetime.AddSeconds(-MAX_DURATION_SEC));
                Model.DefaultXAxis.Maximum = DateTimeAxis.ToDouble(datetime);

                NotifyPropertyChanged(nameof(CurrentStart));
            }

            Model.InvalidatePlot(true);
            MiniModel.InvalidatePlot(true);
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void AddAndMergePoints(ObservableCollection<DataPoint> series, long sum)
        {
            series.Add(DateTimeAxis.CreateDataPoint(datetime, sum));
            if (series.Count > 3 && series[^2].Y == sum && series[^3].Y == sum)
            {
                series.RemoveAt(series.Count - 2);
            }
        }

        private void ResetZoom(object sender, RoutedEventArgs e)
        {
            isXPanned = false;
            chart.ResetAllAxes();
        }

        private void scrollArea_DragStarted(object sender, DragStartedEventArgs e)
        {
            isXPanned = true;
        }
    }
}
