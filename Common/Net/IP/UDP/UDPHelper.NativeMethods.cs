using System;
using System.Runtime.InteropServices;
using Wokhan.WindowsFirewallNotifier.Common.Helpers.IPHelpers;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.UDP
{
    public partial class UDPHelper
    {
        protected new static class NativeMethods
        {
            [DllImport("iphlpapi.dll", SetLastError = true)]
            internal static extern uint GetOwnerModuleFromUdpEntry(ref MIB_UDPROW_OWNER_MODULE pUdpEntry, IPHelper.TCPIP_OWNER_MODULE_INFO_CLASS Class, IntPtr Buffer, ref uint pdwSize);

            [DllImport("iphlpapi.dll", SetLastError = true)]
            internal static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref int dwOutBufLen, bool sort, AF_INET ipVersion, UDP_TABLE_CLASS tblClass, uint reserved);
        }
    }
}
