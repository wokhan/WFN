using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace Wokhan.WindowsFirewallNotifier.Console.ViewModels;

public partial class GroupedMonitoredConnections : ObservableObject, IComparable
{
    public string FileName { get; init; }
    public string Path { get; init; }
    public string? ProductName { get; init; }
    public uint ProcessId { get; init; }

    private SolidColorBrush? _brush;
    public SolidColorBrush Brush => _brush ??= new SolidColorBrush(Color);

    public Color Color { get; set; }

    [ObservableProperty]
    private ulong _inboundBandwidth;

    [ObservableProperty]
    private ulong _outboundBandwidth;

    public GroupedMonitoredConnections(MonitoredConnection connection, Color color)
    {
        
        Path = connection.Path!;
        FileName = connection.FileName!;
        ProductName = connection.ProductName;
        ProcessId = connection.Pid;
        Color = color;

        connection.Color = color;
    }

    public int CompareTo(object? obj)
    {
        return Path.CompareTo((obj as GroupedMonitoredConnections)?.Path);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (ReferenceEquals(obj, null))
        {
            return false;
        }

        return Path.Equals((obj as GroupedMonitoredConnections)?.Path);
    }

    public override int GetHashCode()
    {
        return Path.GetHashCode();
    }
}

