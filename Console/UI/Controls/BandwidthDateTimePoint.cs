using Wokhan.WindowsFirewallNotifier.Console.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Controls;

public record BandwidthDateTimePoint(DateTime DateTime, ulong? BandwidthIn = null, ulong? BandwidthOut = null, GroupedMonitoredConnections? Source = null);