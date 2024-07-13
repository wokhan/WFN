using LiveChartsCore;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;

using SkiaSharp;
using SkiaSharp.Views.WPF;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

using Wokhan.Collections;
using Wokhan.Core;
using Wokhan.WindowsFirewallNotifier.Console.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Controls;

public partial class BandwidthGraph : UserControl, INotifyPropertyChanged
{

    private const int MAX_DURATION_SEC = 10;

    private ObservableDictionary<string, Tuple<ObservableCollection<BandwidthDateTimePoint>, ObservableCollection<BandwidthDateTimePoint>>> allSeries = new();

    private DateTime datetime = DateTime.Now;

    private bool isXPanned;

    private ObservableCollection<BandwidthDateTimePoint> seriesInTotal = new();

    private ObservableCollection<BandwidthDateTimePoint> seriesOutTotal = new();

    private Axis xAxis;
    private Axis yAxis;
    private SolidColorPaint crosshairPaint;

#pragma warning disable CS8618 // Non nullable fields are initialized in InitAxes and InitMiniGraph.
    public BandwidthGraph()
#pragma warning restore CS8618 
    {
        InitMiniGraph();

        InitializeComponent();

        this.Loaded += (o, s) => InitAxes();
    }

    private void InitMiniGraph()
    {
        MiniSeries = new List<ISeries> {
            new LineSeries<BandwidthDateTimePoint>() { Name = "In", Stroke = new SolidColorPaint(SKColors.LightGreen), GeometryStroke = null, GeometryFill = null, Values = seriesInTotal, Mapping = logMapper },
            new LineSeries<BandwidthDateTimePoint>() { Name = "Out", Stroke = new SolidColorPaint(SKColors.OrangeRed), GeometryStroke = null, GeometryFill = null, Values = seriesOutTotal, Mapping = logMapper }
        };
    }


    private void InitAxes()
    {
        var skAxisPaint = new SolidColorPaint(((SolidColorBrush)Application.Current.Resources[System.Windows.SystemColors.WindowTextBrushKey]).Color.ToSKColor());
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
        yAxis.MinStep = 1;
        yAxis.Labeler = (value) => value == 0 ? "0bps" : UnitFormatter.FormatValue(Math.Pow(10, value), "bps");
        yAxis.LabelsPaint = skAxisPaint;
        yAxis.CrosshairPaint = crosshairPaint;

        var miniYAxis = miniChart.YAxes.First();
        miniYAxis.MinLimit = 0;
        miniYAxis.TextSize = 10;
        miniYAxis.Padding = new LiveChartsCore.Drawing.Padding(0);
        miniYAxis.ShowSeparatorLines = false;
        miniYAxis.Labeler = (value) => value == 0 ? "0bps" : UnitFormatter.FormatValue(Math.Pow(10, value), "bps");

        miniChart.XAxes.First().IsVisible = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public double AbsoluteEnd => xAxis?.DataBounds.Max ?? 0;

    public double AbsoluteStart => xAxis?.DataBounds.Min ?? 0;


    public static readonly DependencyProperty ConnectionsProperty = DependencyProperty.Register(nameof(Connections), typeof(GroupedObservableCollection<GroupedMonitoredConnections, MonitoredConnection>), typeof(BandwidthGraph));
    public GroupedObservableCollection<GroupedMonitoredConnections, MonitoredConnection> Connections
    {
        get => (GroupedObservableCollection<GroupedMonitoredConnections, MonitoredConnection>)GetValue(ConnectionsProperty);
        set => SetValue(ConnectionsProperty, value);
    }

    public double CurrentStart
    {
        get => (xAxis is not null ? xAxis.MinLimit + (xAxis.MaxLimit - xAxis.MinLimit) / 2 : 0) ?? 0;
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
    public double ThumbSize => (xAxis is not null ? (xAxis.MaxLimit - xAxis.MinLimit) * scrollArea.Track.ActualWidth / (xAxis.DataBounds.Max - xAxis.DataBounds.Min) : 0) ?? 0;

    private string tooltipFormatter(ChartPoint<BandwidthDateTimePoint, CircleGeometry, LabelGeometry> arg)
    {
        return $"{((LineSeries<BandwidthDateTimePoint>)arg.Context.Series).Tag} - In: {UnitFormatter.FormatValue(arg.PrimaryValue, "bps")} / Out: {UnitFormatter.FormatValue(arg.TertiaryValue, "bps")}";
    }

    private Coordinate logMapper(BandwidthDateTimePoint dateTimePoint, int _)
    {
        var value = (long)(dateTimePoint.BandwidthIn ?? dateTimePoint.BandwidthOut)!;

        // Only points in the first series (IN bandwidth) have to store the secondary values for the legend
        // We keep both in / out values since we don't want the log10 for them but the original values.
        if (dateTimePoint.BandwidthIn is not null)
        {
            return new Coordinate(value == 0 ? 0 : Math.Log10(value), dateTimePoint.DateTime.Ticks, dateTimePoint.BandwidthIn ?? 0, dateTimePoint.BandwidthOut ?? 0, 0, 0, Error.Empty);
        }
        else
        {
            return new Coordinate(value == 0 ? 0 : Math.Log10(value), dateTimePoint.DateTime.Ticks);
        }
    }

    readonly DashEffect outDashEffect = new DashEffect(new[] { 2f, 2f });

    public void UpdateGraph()
    {
        datetime = DateTime.Now;

        var totalIn = 0UL;
        var totalOut = 0UL;

        var localConnections = Dispatcher.Invoke(() => Connections.ToList());
        foreach (var connectionGroup in localConnections)
        {
            ObservableCollection<BandwidthDateTimePoint> seriesInValues;
            ObservableCollection<BandwidthDateTimePoint> seriesOutValues;

            if (allSeries.TryGetValue(connectionGroup.Key.Path, out var seriesValues))
            {
                (seriesInValues, seriesOutValues) = seriesValues;
            }
            else
            {
                seriesInValues = [];
                seriesOutValues = [];
                
                var color = connectionGroup.Key.Color.ToSKColor();
                var inStroke = new SolidColorPaint(color) { StrokeThickness = 2 };
                var outStroke = new SolidColorPaint(color) { StrokeThickness = 2, PathEffect = outDashEffect };

                Series.Add(new LineSeries<BandwidthDateTimePoint>() { Tag = connectionGroup.Key.Path, Name = $"{connectionGroup.Key} - In", XToolTipLabelFormatter = tooltipFormatter, Fill = null, Stroke = inStroke, GeometryStroke = inStroke, Values = seriesInValues, LineSmoothness = 0, Mapping = logMapper });
                Series.Add(new LineSeries<BandwidthDateTimePoint>() { Tag = connectionGroup.Key.Path, Name = $"{connectionGroup.Key} - Out", IsVisibleAtLegend = false, IsHoverable = false, Fill = null, Stroke = outStroke, GeometryStroke = outStroke, Values = seriesOutValues, LineSmoothness = 0, Mapping = logMapper });
             
                allSeries.Add(connectionGroup.Key.Path, Tuple.Create(seriesInValues, seriesOutValues));
            }

            AddAndMergePoints(seriesInValues, connectionGroup.Key.InboundBandwidth, connectionGroup.Key.OutboundBandwidth);
            AddAndMergePoints(seriesOutValues, bandwidthOut: connectionGroup.Key.OutboundBandwidth);

            totalIn += connectionGroup.Key.InboundBandwidth;
            totalOut += connectionGroup.Key.OutboundBandwidth;
        }

        seriesInTotal.Add(new BandwidthDateTimePoint(datetime, BandwidthIn: totalIn));
        seriesOutTotal.Add(new BandwidthDateTimePoint(datetime, BandwidthOut: totalOut));

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

    private void AddAndMergePoints(ObservableCollection<BandwidthDateTimePoint> series, ulong? bandwidthIn = null, ulong? bandwidthOut = null)
    {
        if (series.Count == 0
            || (bandwidthIn == 0 && (series[^1].BandwidthIn != 0 || bandwidthOut is not null and not 0))
            || (bandwidthOut == 0 && series[^1].BandwidthOut != 0))
        {
            series.Add(new BandwidthDateTimePoint(datetime, bandwidthIn, bandwidthOut));
            //if (series.Count > 3 && series[^2].Value == sum && series[^3].Value == sum)
            //{
            //    series.RemoveAt(series.Count - 2);
            //}
        }
    }

    private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
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

    private void XAxis_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is Axis && (e.PropertyName == nameof(xAxis.MinLimit) || e.PropertyName == nameof(xAxis.MaxLimit)))
        {
            NotifyPropertyChanged(nameof(ThumbSize));
            NotifyPropertyChanged(nameof(CurrentStart));
        }
    }

    private void chart_MouseEnter(object sender, MouseEventArgs e)
    {
        chart.AutoUpdateEnabled = false;
        xAxis.CrosshairPaint = crosshairPaint;
        yAxis.CrosshairPaint = crosshairPaint;
    }

    private void chart_MouseLeave(object sender, MouseEventArgs e)
    {
        chart.AutoUpdateEnabled = true;
        xAxis.CrosshairPaint = null;
        yAxis.CrosshairPaint = null;
    }
}
