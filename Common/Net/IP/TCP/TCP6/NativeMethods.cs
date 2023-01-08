using System;
using System.Runtime.InteropServices;
using System.Text;


namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.TCP.TCP6;

internal static partial class NativeMethods
{
    [DllImport("iphlpapi.dll", SetLastError = true)]
    internal static extern uint GetOwnerModuleFromTcp6Entry(MIB_TCP6ROW_OWNER_MODULE pTcpEntry, TCPIP_OWNER_MODULE_INFO_CLASS Class, IntPtr Buffer, ref uint pdwSize);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    internal static extern uint GetPerTcp6ConnectionEStats(MIB_TCP6ROW Row, TCP_ESTATS_TYPE EstatsType, IntPtr Rw, uint RwVersion, uint RwSize, IntPtr Ros, uint RosVersion, uint RosSize, IntPtr Rod, uint RodVersion, uint RodSize);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    internal static extern uint SetPerTcp6ConnectionEStats(MIB_TCP6ROW Row, TCP_ESTATS_TYPE EstatsType, IntPtr Rw, uint RwVersion, uint RwSize, uint Offset);

    [LibraryImport("ntdll.dll", SetLastError = true/*TODO: test this, CharSet = CharSet.Unicode*/)]
    internal static unsafe partial void RtlIpv6AddressToString(byte[] Addr, char* res);
}
