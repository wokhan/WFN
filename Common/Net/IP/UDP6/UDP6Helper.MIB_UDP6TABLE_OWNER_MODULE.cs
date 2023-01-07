using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.UDP6;

public partial class UDP6Helper
{
    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_UDP6TABLE_OWNER_MODULE
    {
        public uint NumEntries;
        public MIB_UDP6ROW_OWNER_MODULE FirstEntry;
    }
}
