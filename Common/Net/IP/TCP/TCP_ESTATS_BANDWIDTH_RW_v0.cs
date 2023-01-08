using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.TCP;

[StructLayout(LayoutKind.Sequential)]
internal struct TCP_ESTATS_BANDWIDTH_RW_v0
{
    internal TCP_BOOLEAN_OPTIONAL EnableCollectionOutbound;
    internal TCP_BOOLEAN_OPTIONAL EnableCollectionInbound;
}
