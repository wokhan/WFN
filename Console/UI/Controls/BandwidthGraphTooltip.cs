using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using LiveChartsCore.VisualElements;

using SkiaSharp.Views.WPF;

using System.Windows;
using System.Windows.Media;

using Wokhan.Core;
using Wokhan.WindowsFirewallNotifier.Console.ViewModels;


namespace Wokhan.WindowsFirewallNotifier.Console.UI.Controls;

/// <summary>
/// Modified from https://livecharts.dev/docs/winforms/2.0.0-rc1/CartesianChart.Tooltips#customize-default-tooltips
/// </summary>
public class BandwidthGraphTooltip : IChartTooltip<SkiaSharpDrawingContext>
{
    private readonly StackPanel<RoundedRectangleGeometry, SkiaSharpDrawingContext> _stackPanel;

    private static readonly int s_zIndex = 10100;
    
    private SolidColorPaint _fontPaint = new(((Color)Application.Current.Resources[SystemColors.InfoTextColorKey]).ToSKColor());

    public BandwidthGraphTooltip()
    {
        _stackPanel = new StackPanel<RoundedRectangleGeometry, SkiaSharpDrawingContext>
        {
            Padding = new Padding(5),
            Orientation = ContainerOrientation.Vertical,
            HorizontalAlignment = Align.Start,
            VerticalAlignment = Align.Middle,
            BackgroundPaint = new SolidColorPaint(((Color)Application.Current.Resources[SystemColors.InfoColorKey]).ToSKColor())
        };

        _stackPanel.Animate(new Animation(EasingFunctions.BounceOut, TimeSpan.FromSeconds(1)), nameof(_stackPanel.X), nameof(_stackPanel.Y));
    }

    public void UpdateColors()
    {
        // Should update all visuals?
        _fontPaint = new(((Color)Application.Current.Resources[SystemColors.InfoTextColorKey]).ToSKColor());
        _stackPanel.BackgroundPaint = new SolidColorPaint(((Color)Application.Current.Resources[SystemColors.InfoColorKey]).ToSKColor());
    }

    public void Show(IEnumerable<ChartPoint> foundPoints, Chart<SkiaSharpDrawingContext> chart)
    {
        // clear the previous elements.
        foreach (var child in _stackPanel.Children.ToArray())
        {
            _ = _stackPanel.Children.Remove(child);
            chart.RemoveVisual(child);
        }
        //_stackPanel.Children.Clear();

        foreach (var point in foundPoints)
        {
            var bandwidthDateTimePoint = (BandwidthDateTimePoint)point.Context.DataSource!;

            // Ignore secondary (out) points as the bandwidth is already logged along the "In" series
            if (bandwidthDateTimePoint.BandwidthIn is null)
            {
                continue;
            }

            var sketch = ((IChartSeries<SkiaSharpDrawingContext>)point.Context.Series).GetMiniaturesSketch();
            var relativePanel = sketch.AsDrawnControl(s_zIndex);

            var groupedConnections = bandwidthDateTimePoint.Source!;

            var label = new LabelVisual
            {
                Text = $"{groupedConnections.FileName} ({groupedConnections.ProcessId}) - In: {UnitFormatter.FormatValue(bandwidthDateTimePoint.BandwidthIn ?? 0, "bps")} / Out: {UnitFormatter.FormatValue(bandwidthDateTimePoint.BandwidthOut ?? 0, "bps")}",
                Paint = _fontPaint,
                TextSize = 12,
                Padding = new Padding(8, 0, 0, 0),
                ClippingMode = ClipMode.None, // required on tooltips 
                VerticalAlignment = Align.Start,
                HorizontalAlignment = Align.Start
            };

            var sp = new StackPanel<RoundedRectangleGeometry, SkiaSharpDrawingContext>
            {
                Padding = new Padding(0, 4),
                VerticalAlignment = Align.Middle,
                HorizontalAlignment = Align.Middle,
                Children = { relativePanel, label }
            };

            _stackPanel.Children.Add(sp);
        }

        var size = _stackPanel.Measure(chart);

        var location = foundPoints.GetTooltipLocation(size, chart);

        _stackPanel.X = location.X;
        _stackPanel.Y = location.Y;
        
        chart.AddVisual(_stackPanel);
    }

    public void Hide(Chart<SkiaSharpDrawingContext> chart)
    {
        //if (chart is null || _stackPanel is null) return;
        chart.RemoveVisual(_stackPanel);
    }
}