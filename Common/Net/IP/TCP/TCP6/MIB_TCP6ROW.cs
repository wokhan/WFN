using System;
using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.TCP.TCP6;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
internal struct MIB_TCP6ROW : ITcpRow
{
    internal ConnectionStatus State;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    internal byte[] _localAddress;
    internal uint LocalScopeId;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    internal byte[] _localPort;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    internal byte[] _remoteAddress;
    internal uint RemoteScopeId;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    internal byte[] _remotePort;

    public void EnsureStats() => TCPHelper.EnsureStatsAreEnabledInternal(NativeMethods.SetPerTcp6ConnectionEStats, this);

    public TCP_ESTATS_BANDWIDTH_ROD_v0 GetTCPBandwidth() => TCPHelper.GetTCPBandwidthInternal(NativeMethods.GetPerTcp6ConnectionEStats, this);
}
