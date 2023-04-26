using System;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Controls;

public record CustomDateTimePoint(DateTime DateTime, ulong Value, ulong BandwidthIn = 0, ulong BandwidthOut = 0);