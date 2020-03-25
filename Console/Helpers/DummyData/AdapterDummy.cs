using System.Collections.Generic;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.DummyData
{
    public class AdapterDummy
    {
        public class IPInterfaceStatistics
        {
            public long BytesReceived => 10000;
            public long BytesSent => 10000;
            public long IncomingPacketsDiscarded => 10000;
            public long IncomingPacketsWithErrors => 10000;
            public long IncomingUnknownProtocolPackets => 10000;
            public long NonUnicastPacketsReceived => 10000;
            public long NonUnicastPacketsSent => 10000;
            public long OutgoingPacketsDiscarded => 10000;
            public long OutgoingPacketsWithErrors => 10000;
            public long OutputQueueLength => 10000;
            public long UnicastPacketsReceived => 10000;
            public long UnicastPacketsSent => 10000;
        }

        public class IPInterfaceProperties
        {
            public List<string> AnycastAddresses => new List<string> { "12345" };
            public List<string> DhcpServerAddresses => new List<string> { "12345" };
            public List<string> DnsAddresses => new List<string> { "12345" };
            public string DnsSuffix => "";
            public List<string> GatewayAddresses => new List<string> { "12345" };
            public bool IsDnsEnabled => true;
            public bool IsDynamicDnsEnabled => false;
            public List<string> MulticastAddresses => new List<string> { "12345" };
            public List<string> UnicastAddresses => new List<string> { "12345" };
            public List<string> WinsServersAddresses => new List<string> { "12345" };
        }

        public class NetworkInterface
        {
            public string Description => "Test interface";
            public string Id => "ID";
            public bool IsReceiveOnly => false;
            public string Name => "Name";
            public string NetworkInterfaceType => "Fake interface";
            public string OperationalStatus => "Online";
            public long Speed => 1000000;
        }

        public NetworkInterface Information => new NetworkInterface();

        public IPInterfaceStatistics Statistics => new IPInterfaceStatistics();

        public IPInterfaceProperties Properties => new IPInterfaceProperties();

        public AdapterDummy()
        {

        }
    }
}