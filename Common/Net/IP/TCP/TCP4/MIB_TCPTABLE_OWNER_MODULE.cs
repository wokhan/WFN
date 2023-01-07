using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.TCP;

public partial class TCPHelper
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct MIB_TCPTABLE_OWNER_MODULE
    {
        public uint NumEntries;
        public MIB_TCPROW_OWNER_MODULE FirstEntry;
    }
}
