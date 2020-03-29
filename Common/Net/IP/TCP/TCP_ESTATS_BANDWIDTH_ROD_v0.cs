namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP
{
    public partial class TCPHelper
    {
        public struct TCP_ESTATS_BANDWIDTH_ROD_v0
        {
            public ulong OutboundBandwidth;
            public ulong InboundBandwidth;
            public ulong OutboundInstability;
            public ulong InboundInstability;
            public bool OutboundBandwidthPeaked;
            public bool InboundBandwidthPeaked;
        }
    }
}
