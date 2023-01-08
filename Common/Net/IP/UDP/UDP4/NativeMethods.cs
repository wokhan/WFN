using System;
using System.Runtime.InteropServices;

using static Wokhan.WindowsFirewallNotifier.Common.Net.IP.IPHelper;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.UDP.UDP4;

internal static partial class NativeMethods
{
    [DllImport("iphlpapi.dll", SetLastError = true)]
    internal static extern uint GetOwnerModuleFromUdpEntry(MIB_UDPROW_OWNER_MODULE pUdpEntry, TCPIP_OWNER_MODULE_INFO_CLASS Class, IntPtr Buffer, ref uint pdwSize);

}
