using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace Wokhan.WindowsFirewallNotifier.Console.ViewModels;

public partial class ExposedInterfaceView : ObservableObject
{
    public string MAC { get; init; }

    [ObservableProperty]
    private NetworkInterface _information;

    [ObservableProperty]
    public IPInterfaceStatistics _statistics;

    [ObservableProperty]
    private IPInterfaceProperties _properties;
    
    [ObservableProperty]
    private ComputedBandwidth _bandwidth = new();

    private readonly int delay = 400; // Arbitrary delay, must be lower than the Adapters page timer
    private readonly Stopwatch sw = new();
    private async Task ComputeBandwidth()
    {
        var initial = Information.GetIPStatistics();
        sw.Restart();
        await Task.Delay(delay);
        var final = Information.GetIPStatistics();
        sw.Stop();

        var inb = AdjustBandwidth(initial.BytesReceived, final.BytesReceived, sw.ElapsedMilliseconds);
        var outb = AdjustBandwidth(initial.BytesSent, final.BytesSent, sw.ElapsedMilliseconds);

        Bandwidth = new ComputedBandwidth(inb, outb);
    }

    private static double AdjustBandwidth(long bytes1, long bytes2, long elapsedms)
    {
        return Math.Max(0, (bytes2 - bytes1) * 8 / elapsedms * 1000);
    }

    public record ComputedBandwidth(double In = 0, double Out = 0);

    public ExposedInterfaceView(NetworkInterface netInterface)
    {
        this.MAC = String.Join(":", netInterface.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2")));
        
        UpdateInner(netInterface);
    }

    internal void UpdateInner(NetworkInterface netInterface)
    {
        Information = netInterface;

        _ = ComputeBandwidth();

        Statistics = Information.GetIPStatistics();
        Properties = Information.GetIPProperties();
    }
}
