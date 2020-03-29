using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP
{
    public partial class TCP6Helper 
    {
        protected new static class NativeMethods
        {
            [DllImport("iphlpapi.dll", SetLastError = true)]
            internal static extern uint GetOwnerModuleFromTcp6Entry(ref MIB_TCP6ROW_OWNER_MODULE pTcpEntry, TCPIP_OWNER_MODULE_INFO_CLASS Class, IntPtr Buffer, ref uint pdwSize);

            [DllImport("iphlpapi.dll", SetLastError = true)]
            internal static extern uint GetPerTcp6ConnectionEStats(ref MIB_TCP6ROW Row, TCP_ESTATS_TYPE EstatsType, IntPtr Rw, uint RwVersion, uint RwSize, IntPtr Ros, uint RosVersion, uint RosSize, IntPtr Rod, uint RodVersion, uint RodSize);

            [DllImport("iphlpapi.dll", SetLastError = true)]
            internal static extern uint SetPerTcp6ConnectionEStats(ref MIB_TCP6ROW Row, TCP_ESTATS_TYPE EstatsType, IntPtr Rw, uint RwVersion, uint RwSize, uint Offset);

            [DllImport("ntdll.dll", SetLastError = true/*TODO: test this, CharSet = CharSet.Unicode*/)] 
            internal static extern void RtlIpv6AddressToString(byte[] Addr, out StringBuilder res);
        }
    }
}