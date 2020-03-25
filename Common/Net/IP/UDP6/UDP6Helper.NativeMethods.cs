using System;
using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.UDP6
{
    public partial class UDP6Helper
    {
        protected new static class NativeMethods
        {
            [DllImport("iphlpapi.dll", SetLastError = true)]
            internal static extern uint GetOwnerModuleFromUdp6Entry(ref MIB_UDP6ROW_OWNER_MODULE pUdpEntry, TCPIP_OWNER_MODULE_INFO_CLASS Class, IntPtr Buffer, ref uint pdwSize);
        }
    }
}