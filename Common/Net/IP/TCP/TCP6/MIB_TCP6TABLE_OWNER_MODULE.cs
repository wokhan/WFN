using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.TCP.TCP6;

[StructLayout(LayoutKind.Sequential)]
internal struct MIB_TCP6TABLE_OWNER_MODULE
{
    public uint NumEntries;
    public MIB_TCP6ROW_OWNER_MODULE FirstEntry;
}