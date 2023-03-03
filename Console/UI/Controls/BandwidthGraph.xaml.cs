using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;

using SkiaSharp;
using SkiaSharp.Views.WPF;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

using Wokhan.Collections;
using Wokhan.WindowsFirewallNotifier.Console.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Controls;

public partial class BandwidthGraph : UserControl, INotifyPropertyChanged
{
    public static readonly DependencyProperty ConnectionsProperty = DependencyProperty.Register(nameof(Connections), typeof(ObservableCollection<Connection>), typeof(BandwidthGraph));

    private const int MAX_DURATION_SEC = 10;

    private ObservableDictionary<string, Tuple<ObservableCollection<DateTimePoint>, ObservableCollection<DateTimePoint>>> allSeries = new();

    private DateTime datetime = DateTime.Now;

    private bool isXPanned;

    private Action<DateTimePoint, ChartPoint> logMapper = (logPoint, chartPoint) =>
    {
        chartPoint.SecondaryValue = logPoint.DateTime.Ticks;
        chartPoint.PrimaryValue = Math.Log((double)logPoint.Value, 10);
    };

    private ObservableCollection<DateTimePoint> seriesInTotal = new();

    private ObservableCollection<DateTimePoint> seriesOutTotal = new();

    private Axis xAxis;
    private Axis yAxis;
    private SolidColorPaint crosshairPaint;

    public BandwidthGraph()
    {
        InitMiniGraph();

        InitializeComponent();

        InitAxes();
    }

    private void InitMiniGraph()
    {
        MiniSeries = new List<ISeries> {
            new LineSeries<DateTimePoint>() { Name = "In", Stroke = new SolidColorPaint(SKColors.LightGreen), GeometryStroke = null, GeometryFill = null, Values = seriesInTotal, Mapping = logMapper },
            new LineSeries<DateTimePoint>() { Name = "Out", Stroke = new SolidColorPaint(SKColors.OrangeRed), GeometryStroke = null, GeometryFill = null, Values = seriesOutTotal, Mapping = logMapper }
        };
    }

    private void InitAxes()
    {
        var skAxisPaint = new SolidColorPaint(((SolidColorBrush)Application.Current.Resources[SystemColors.WindowTextBrushKey]).Color.ToSKColor());
        crosshairPaint = new SolidColorPaint(SKColors.Red) { StrokeThickness = 1 };

        xAxis = (Axis)chart.XAxes.First();
        xAxis.Labeler = (time) => new DateTime((long)time).ToString("HH:mm:ss");
        xAxis.TextSize = 10;
        xAxis.LabelsPaint = skAxisPaint;
        xAxis.CrosshairPaint = crosshairPaint;

        xAxis.PropertyChanged += XAxis_PropertyChanged;

        yAxis = (Axis)chart.YAxes.First();
        yAxis.MinLimit = 0;
        yAxis.TextSize = 10;
        yAxis.Labeler = (value) => Math.Pow(10, value).ToString(); //value == 0 ? "0Bps" : UnitFormatter.FormatBytes(Math.Pow(10, value), "ps");
        yAxis.LabelsPaint = skAxisPaint;
        yAxis.CrosshairPaint = crosshairPaint;

        var miniYAxis = miniChart.YAxes.First();
        miniYAxis.MinLimit = 0;
        miniYAxis.TextSize = 10;
        miniYAxis.Padding = new LiveChartsCore.Drawing.Padding(0);
        miniYAxis.ShowSeparatorLines = false;
        miniYAxis.Labeler = (value) => Math.Pow(10, value).ToString();

        miniChart.XAxes.First().IsVisible = false;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public double AbsoluteEnd => xAxis?.DataBounds.Max ?? 0;

    public double AbsoluteStart => xAxis?.DataBounds.Min ?? 0;

    public ObservableCollection<Connection> Connections { get => (ObservableCollection<Connection>)GetValue(ConnectionsProperty); set => SetValue(ConnectionsProperty, value); }

    public double CurrentStart
    {
        get => (double)(xAxis is not null ? xAxis.MinLimit + (xAxis.MaxLimit - xAxis.MinLimit) / 2 : 0);
        set
        {
            if (isXPanned)
            {
                var window = (xAxis.MaxLimit - xAxis.MinLimit) / 2;
                xAxis.MinLimit = value - window;
                xAxis.MaxLimit = value + window;

                NotifyPropertyChanged();
            }
        }
    }
    public IEnumerable<ISeries> MiniSeries { get; private set; }
    public ObservableCollection<ISeries> Series { get; } = new();
    public double ThumbSize => (double)(xAxis is not null ? (xAxis.MaxLimit - xAxis.MinLimit) * scrollArea.Track.ActualWidth / (xAxis.DataBounds.Max - xAxis.DataBounds.Min) : 0);

    public void UpdateGraph()
    {
        datetime = DateTime.Now;

        //Creates a copy of the current connections list to avoid grouping to occur on the UI thread.
        var localConnections = Dispatcher.Invoke(() => Connections.ToList()).GroupBy(connection => $"{connection.ProductName} ({connection.Owner} / {connection.Pid})");
        long currentIn = 0;
        long currentOut = 0;
        foreach (var connectionGroup in localConnections.AsParallel())
        {
            ObservableCollection<DateTimePoint> seriesInValues;
            ObservableCollection<DateTimePoint> seriesOutValues;

            if (allSeries.TryGetValue(connectionGroup.Key, out var seriesValues))
            {
                (seriesInValues, seriesOutValues) = seriesValues;
            }
            else
            {
                seriesInValues = new();
                seriesOutValues = new();

                var color = connectionGroup.First().Color.ToSKColor();
                Series.Add(new LineSeries<DateTimePoint>() { Name = $"{connectionGroup.Key} - In", Fill = null, Stroke = new SolidColorPaint(color) { StrokeThickness = 2 }, Values = seriesInValues, Mapping = logMapper, LineSmoothness = 0 });
                Series.Add(new LineSeries<DateTimePoint>() { Name = $"{connectionGroup.Key} - Out", Fill = null, Stroke = new SolidColorPaint(color) { StrokeThickness = 2, PathEffect = new DashEffect(new[] { 2f, 2f }) }, Values = seriesOutValues, Mapping = logMapper, LineSmoothness = 0 });

                allSeries.Add(connectionGroup.Key, Tuple.Create(seriesInValues, seriesOutValues));
            }

            var lastSumIn = connectionGroup.Sum(connection => (long)connection.InboundBandwidth);
            currentIn += lastSumIn;
            AddAndMergePoints(seriesInValues, lastSumIn);

            var lastSumOut = connectionGroup.Sum(connection => (long)connection.OutboundBandwidth);
            currentOut += lastSumOut;
            AddAndMergePoints(seriesOutValues, lastSumOut);
        }

        seriesInTotal.Add(new DateTimePoint(datetime, currentIn));
        seriesOutTotal.Add(new DateTimePoint(datetime, currentOut));

        NotifyPropertyChanged(nameof(AbsoluteStart));
        NotifyPropertyChanged(nameof(AbsoluteEnd));
        NotifyPropertyChanged(nameof(ThumbSize));

        // If scrolling has not been manually forced
        if (!isXPanned)
        {
            Dispatcher.Invoke(() =>
            {
                xAxis.MinLimit = datetime.AddSeconds(-MAX_DURATION_SEC).Ticks;
                // Sets the max position to 1/10th of the max duration value to keep some padding
                xAxis.MaxLimit = datetime.AddSeconds(MAX_DURATION_SEC / 10).Ticks;
            });
            NotifyPropertyChanged(nameof(CurrentStart));
        }
    }

    private void AddAndMergePoints(ObservableCollection<DateTimePoint> series, long sum)
    {
        if (sum != 0 || series.Count == 0 || series[^1].Value != 0)
        {
            series.Add(new DateTimePoint(datetime, sum));
            //if (series.Count > 3 && series[^2].Value == sum && series[^3].Value == sum)
            //{
            //    series.RemoveAt(series.Count - 2);
            //}
        }
    }

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void ResetZoom(object sender, RoutedEventArgs e)
    {
        isXPanned = false;
    }

    private void scrollArea_DragStarted(object sender, DragStartedEventArgs e)
    {
        isXPanned = true;
    }

    private void XAxis_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (sender is Axis && (e.PropertyName == nameof(xAxis.MinLimit) || e.PropertyName == nameof(xAxis.MaxLimit)))
        {
            NotifyPropertyChanged(nameof(ThumbSize));
            NotifyPropertyChanged(nameof(CurrentStart));
        }
    }

    private void chart_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        chart.AutoUpdateEnabled = false;
        xAxis.CrosshairPaint = crosshairPaint;
        yAxis.CrosshairPaint = crosshairPaint;
    }

    private void chart_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        chart.AutoUpdateEnabled = true;
        xAxis.CrosshairPaint = null;
        yAxis.CrosshairPaint = null;
    }
}
