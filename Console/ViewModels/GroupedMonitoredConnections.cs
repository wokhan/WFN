using CommunityToolkit.Mvvm.ComponentModel;

using System.Linq;
using System.Windows.Media;

using Wokhan.Collections;

namespace Wokhan.WindowsFirewallNotifier.Console.ViewModels;

public partial class GroupedMonitoredConnections : ObservableObject
{
    public GroupedMonitoredConnections(MonitoredConnection connection, Color color)
    {
        Path = connection.Path!;
        FileName = connection.FileName!;
        ProductName = connection.ProductName;
        Color = color;
    }

    public string FileName { get; init; }
    public string Path { get; init; }
    public string? ProductName { get; init; }
    public Color Color { get; set; }

    [ObservableProperty]
    private long _inboundBandwidth;

    [ObservableProperty]
    private long _outboundBandwidth;

    public void UpdateBandwidth(ObservableGrouping<GroupedMonitoredConnections, MonitoredConnection> group)
    {
        InboundBandwidth = group.Sum(connection => (long)connection.InboundBandwidth);
        OutboundBandwidth = group.Sum(connection => (long)connection.OutboundBandwidth);
    }

    public override int GetHashCode()
    {
        return Path.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return Path.Equals(((GroupedMonitoredConnections)obj).Path);
    }
}

