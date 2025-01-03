using LiveChartsCore;
using LiveChartsCore.Drawing;
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

    private readonly ObservableDictionary<string, Tuple<ObservableCollection<BandwidthDateTimePoint>, ObservableCollection<BandwidthDateTimePoint>>> allSeries = [];

    private DateTime datetime = DateTime.Now;

    private bool isXPanned;

    private readonly ObservableCollection<BandwidthDateTimePoint> seriesInTotal = [];

    private readonly ObservableCollection<BandwidthDateTimePoint> seriesOutTotal = [];

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
        MiniSeries = [
            new LineSeries<BandwidthDateTimePoint>() { Name = "In", Stroke = new SolidColorPaint(SKColors.LightGreen), GeometryStroke = null, GeometryFill = null, Values = seriesInTotal, Mapping = LogMapper },
            new LineSeries<BandwidthDateTimePoint>() { Name = "Out", Stroke = new SolidColorPaint(SKColors.OrangeRed), GeometryStroke = null, GeometryFill = null, Values = seriesOutTotal, Mapping = LogMapper }
        ];
    }


    private void InitAxes()
    {
        var skAxisPaint = new SolidColorPaint(((Color)Application.Current.Resources[SystemColors.WindowTextColorKey]).ToSKColor());
        crosshairPaint = new SolidColorPaint(SKColors.Red) { StrokeThickness = 1 };

        xAxis = (Axis)chart.XAxes.First();
        xAxis.Labeler = (time) => new DateTime((long)time).ToString("HH:mm:ss");
        xAxis.TextSize = 10;
        xAxis.LabelsPaint = skAxisPaint;
        xAxis.CrosshairPaint = crosshairPaint;

        xAxis.PropertyChanged += XAxis_PropertyChanged;

        yAxis = (Axis)chart.YAxes.First();
        yAxis.MinLimit = 0;
        yAxis.MinStep = 10;
        yAxis.TextSize = 10;
        yAxis.Labeler = (value) => value == 0 ? "0bps" : UnitFormatter.FormatValue(Math.Pow(10, value), "bps");
        yAxis.LabelsPaint = skAxisPaint;
        yAxis.CrosshairPaint = crosshairPaint;

        var miniYAxis = miniChart.YAxes.First();
        miniYAxis.MinLimit = 0;
        miniYAxis.MinStep = 10;
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
    public ObservableCollection<ISeries> Series { get; } = [];
    public double ThumbSize => (xAxis is not null ? (xAxis.MaxLimit - xAxis.MinLimit) * scrollArea.Track.ActualWidth / (xAxis.DataBounds.Max - xAxis.DataBounds.Min) : 0) ?? 0;

    private Coordinate LogMapper(BandwidthDateTimePoint dateTimePoint, int _)
    {
        var value = dateTimePoint.BandwidthIn ?? dateTimePoint.BandwidthOut ?? 0;
        return new Coordinate(dateTimePoint.DateTime.Ticks, value == 0 ? 0 : Math.Log10(value));
    }

    readonly DashEffect outDashEffect = new([2f, 2f]);

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
                
                Series.Add(new LineSeries<BandwidthDateTimePoint>() { Tag = connectionGroup.Key, Name = $"{connectionGroup.Key.FileName} ({connectionGroup.Key.ProcessId}) - In", Fill = null, Stroke = inStroke, GeometryStroke = inStroke, Values = seriesInValues, LineSmoothness = 0, Mapping = LogMapper });
                Series.Add(new LineSeries<BandwidthDateTimePoint, SquareGeometry>() { Name = $"{connectionGroup.Key.FileName} ({connectionGroup.Key.ProcessId}) - Out", Fill = null, Stroke = outStroke, GeometryStroke = outStroke, Values = seriesOutValues, LineSmoothness = 0, Mapping = LogMapper });
                
                allSeries.Add(connectionGroup.Key.Path, Tuple.Create(seriesInValues, seriesOutValues));
            }

            AddAndMergePoints(connectionGroup.Key, seriesInValues, seriesOutValues);

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


    private void AddAndMergePoints(GroupedMonitoredConnections group, ObservableCollection<BandwidthDateTimePoint> seriesIn, ObservableCollection<BandwidthDateTimePoint> seriesOut)
    {
        bool isOut = group.OutboundBandwidth != 0 || (seriesOut.Count > 0 && seriesOut[^1].BandwidthOut != 0);
        if (isOut || group.OutboundBandwidth != 0 || group.InboundBandwidth != 0 || (seriesIn.Count > 0 && seriesIn[^1].BandwidthIn != 0))
        {
            // Adding both inbound and outbound bandwidth as they will be used in the tooltip
            seriesIn.Add(new BandwidthDateTimePoint(datetime, group.InboundBandwidth, group.OutboundBandwidth, group));
        }

        if (isOut)
        {
            seriesOut.Add(new BandwidthDateTimePoint(datetime, BandwidthOut: group.OutboundBandwidth));
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
