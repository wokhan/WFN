using static Wokhan.WindowsFirewallNotifier.Common.Net.IP.TCP.TCPHelper;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP.TCP;

public interface ITcpRow
{
    TCP_ESTATS_BANDWIDTH_ROD_v0 GetTCPBandwidth();
    void EnsureStats();
}