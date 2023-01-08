using System;
using System.Runtime.InteropServices;

using static Wokhan.WindowsFirewallNotifier.Common.Net.IP.IPHelper;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.UDP;

internal static partial class NativeMethods
{
    [LibraryImport("iphlpapi.dll", SetLastError = true)]
    internal static partial uint GetExtendedUdpTable(IntPtr pUdpTable, ref int dwOutBufLen, [MarshalAs(UnmanagedType.Bool)] bool sort, AF_INET ipVersion, UDP_TABLE_CLASS tblClass, uint reserved);
}
