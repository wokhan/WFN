using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.UDP.UDP4;

[StructLayout(LayoutKind.Sequential)]
internal struct MIB_UDPTABLE_OWNER_MODULE
{
    public uint NumEntries;
    public MIB_UDPROW_OWNER_MODULE FirstEntry;
}
