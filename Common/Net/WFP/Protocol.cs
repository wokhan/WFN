using NetFwTypeLib;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.WFP
{
    public enum Protocol
    {
        TCP = NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP,
        UDP = NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP,
        ANY = NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY
    }
}