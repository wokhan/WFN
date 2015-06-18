using System.Collections.Generic;
using Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.DummyData
{
    public class AdapterDummy
    {
        public class IPInterfaceStatistics
        {
            public long BytesReceived { get { return 10000; } }
            public long BytesSent { get { return 10000; } }
            public long IncomingPacketsDiscarded { get { return 10000; } }
            public long IncomingPacketsWithErrors { get { return 10000; } }
            public long IncomingUnknownProtocolPackets { get { return 10000; } }
            public long NonUnicastPacketsReceived { get { return 10000; } }
            public long NonUnicastPacketsSent { get { return 10000; } }
            public long OutgoingPacketsDiscarded { get { return 10000; } }
            public long OutgoingPacketsWithErrors { get { return 10000; } }
            public long OutputQueueLength { get { return 10000; } }
            public long UnicastPacketsReceived { get { return 10000; } }
            public long UnicastPacketsSent { get { return 10000; } }
        }

        public class IPInterfaceProperties
        {
            public List<string> AnycastAddresses { get { return new List<string> { "12345" }; } }
            public List<string> DhcpServerAddresses { get { return new List<string> { "12345" }; } }
            public List<string> DnsAddresses { get { return new List<string> { "12345" }; } }
            public string DnsSuffix { get; }
            public List<string> GatewayAddresses { get { return new List<string> { "12345" }; } }
            public bool IsDnsEnabled { get; }
            public bool IsDynamicDnsEnabled { get; }
            public List<string> MulticastAddresses { get { return new List<string> { "12345" }; } }
            public List<string> UnicastAddresses { get { return new List<string> { "12345" }; } }
            public List<string> WinsServersAddresses { get { return new List<string> { "12345" }; } }
        }

        public class NetworkInterface
        {
            public string Description { get { return "Test interface"; } }
            public string Id { get { return "ID"; } }
            public bool IsReceiveOnly { get; }
            public string Name { get { return "Name"; } }
            public string NetworkInterfaceType { get { return "Fake interface"; } }
            public string OperationalStatus { get { return "Online"; } }
            public long Speed { get { return 1000000; } }
        }

        public NetworkInterface Information { get { return new NetworkInterface(); } }

        public IPInterfaceStatistics Statistics { get { return new IPInterfaceStatistics(); } }

        public IPInterfaceProperties Properties { get { return new IPInterfaceProperties(); } }

        public AdapterDummy()
        {

        }
    }
}