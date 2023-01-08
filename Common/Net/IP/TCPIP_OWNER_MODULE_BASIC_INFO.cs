using System;
using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP;

[StructLayout(LayoutKind.Sequential)]
internal struct TCPIP_OWNER_MODULE_BASIC_INFO
{
    public IntPtr p1;
    public IntPtr p2;
}
