using System.Net.NetworkInformation;

namespace Wokhan.WindowsFirewallNotifier.Console.ViewModels
{
    internal class ExposedInterfaceViewDummy : ExposedInterfaceView
    {
        public class DummyIPInterfaceStatistics : IPInterfaceStatistics
        {
            public override long BytesReceived => 10000;
            public override long BytesSent => 10000;
            public override long IncomingPacketsDiscarded => 10000;
            public override long IncomingPacketsWithErrors => 10000;
            public override long IncomingUnknownProtocolPackets => 10000;
            public override long NonUnicastPacketsReceived => 10000;
            public override long NonUnicastPacketsSent => 10000;
            public override long OutgoingPacketsDiscarded => 10000;
            public override long OutgoingPacketsWithErrors => 10000;
            public override long OutputQueueLength => 10000;
            public override long UnicastPacketsReceived => 10000;
            public override long UnicastPacketsSent => 10000;
        }

        public class DummyIPInterfaceProperties : IPInterfaceProperties
        {
            public override IPAddressInformationCollection AnycastAddresses { get; }
            public override IPAddressCollection DhcpServerAddresses { get; }
            public override IPAddressCollection DnsAddresses { get; }
            public override string DnsSuffix => "";
            public override GatewayIPAddressInformationCollection GatewayAddresses { get; }
            public override bool IsDnsEnabled => true;
            public override bool IsDynamicDnsEnabled => false;
            public override MulticastIPAddressInformationCollection MulticastAddresses { get; }// => new MulticastIPAddressInformationCollection { new MulticastIPAddressInformation("12345" };
            public override UnicastIPAddressInformationCollection UnicastAddresses { get; }
            public override IPAddressCollection WinsServersAddresses { get; }

            public override IPv4InterfaceProperties GetIPv4Properties() => null;

            public override IPv6InterfaceProperties GetIPv6Properties() => null;
        }

        public class DummyNetworkInterface : NetworkInterface
        {
            public override string Description => "Test interface";
            public override string Id => "ID";
            public override bool IsReceiveOnly => false;
            public override string Name => "Name";
            public override NetworkInterfaceType NetworkInterfaceType => NetworkInterfaceType.Unknown;
            public override OperationalStatus OperationalStatus => OperationalStatus.Up;
            public override long Speed => 1000000;

            public override IPInterfaceStatistics GetIPStatistics() => new DummyIPInterfaceStatistics();

            public override IPInterfaceProperties GetIPProperties() => new DummyIPInterfaceProperties();
        }

        public new NetworkInterface Information => new DummyNetworkInterface();

        public ExposedInterfaceViewDummy()
        {
        }
    }
}