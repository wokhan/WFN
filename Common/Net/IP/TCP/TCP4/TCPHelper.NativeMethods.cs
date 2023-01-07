using System;
using System.Runtime.InteropServices;

using static Wokhan.WindowsFirewallNotifier.Common.Net.IP.IPHelper;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.TCP;

public partial class TCPHelper
{
    protected new static partial class NativeMethods
    {
        [DllImport("iphlpapi.dll", SetLastError = true)]
        internal static extern uint GetOwnerModuleFromTcpEntry(MIB_TCPROW_OWNER_MODULE pTcpEntry, TCPIP_OWNER_MODULE_INFO_CLASS Class, IntPtr Buffer, ref uint pdwSize);

        [LibraryImport("iphlpapi.dll", SetLastError = true)]
        internal static partial uint GetExtendedTcpTable(IntPtr pTcpTable, ref uint dwOutBufLen, [MarshalAs(UnmanagedType.Bool)] bool sort, AF_INET ipVersion, TCP_TABLE_CLASS tblClass, uint reserved);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        internal static extern uint GetPerTcpConnectionEStats(MIB_TCPROW Row, TCP_ESTATS_TYPE EstatsType, IntPtr Rw, uint RwVersion, uint RwSize, IntPtr Ros, uint RosVersion, uint RosSize, IntPtr Rod, uint RodVersion, uint RodSize);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        internal static extern uint SetPerTcpConnectionEStats(MIB_TCPROW Row, TCP_ESTATS_TYPE EstatsType, IntPtr Rw, uint RwVersion, uint RwSize, uint Offset);

    }
}
