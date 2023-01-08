using System.Runtime.InteropServices;


namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.TCP.TCP4;

[StructLayout(LayoutKind.Sequential)]
internal struct MIB_TCPTABLE_OWNER_MODULE
{
    public uint NumEntries;
    public MIB_TCPROW_OWNER_MODULE FirstEntry;
}
