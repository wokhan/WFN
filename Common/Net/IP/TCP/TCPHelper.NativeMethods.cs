using System;
using System.Runtime.InteropServices;

using static Wokhan.WindowsFirewallNotifier.Common.Net.IP.IPHelper;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.TCP;

internal static partial class NativeMethods
{
    [LibraryImport("iphlpapi.dll", SetLastError = true)]
    internal static partial uint GetExtendedTcpTable(IntPtr pTcpTable, ref uint dwOutBufLen, [MarshalAs(UnmanagedType.Bool)] bool sort, AF_INET ipVersion, TCP_TABLE_CLASS tblClass, uint reserved);

}
