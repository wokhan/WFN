using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.UDP;

public partial class UDPHelper
{
    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_UDPTABLE_OWNER_MODULE
    {
        public uint NumEntries;
        public MIB_UDPROW_OWNER_MODULE FirstEntry;
    }
}
