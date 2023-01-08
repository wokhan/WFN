using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.UDP.UDP6;

[StructLayout(LayoutKind.Sequential)]
internal struct MIB_UDP6TABLE_OWNER_MODULE
{
    public uint NumEntries;
    public MIB_UDP6ROW_OWNER_MODULE FirstEntry;
}
