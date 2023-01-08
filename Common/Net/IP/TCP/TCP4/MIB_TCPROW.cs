using System;
using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.TCP.TCP4;

[StructLayout(LayoutKind.Sequential)]
internal struct MIB_TCPROW : ITcpRow
{
    public ConnectionStatus State;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] dwLocalAddr;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] dwLocalPort;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] dwRemoteAddr;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] dwRemotePort;

    public void EnsureStats() => TCPHelper.EnsureStatsAreEnabledInternal(NativeMethods.SetPerTcpConnectionEStats, this);

    public TCP_ESTATS_BANDWIDTH_ROD_v0 GetTCPBandwidth() => TCPHelper.GetTCPBandwidthInternal(NativeMethods.GetPerTcpConnectionEStats, this);

}
