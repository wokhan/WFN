using System;

using Wokhan.WindowsFirewallNotifier.Common.Net.IP.TCP;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP;

public interface IConnectionOwnerInfo
{
    // CHANGE
    byte[] RemoteAddrBytes { get; }

    string RemoteAddress { get; }
    int RemotePort { get; }
    string LocalAddress { get; }
    int LocalPort { get; }
    Owner? OwnerModule { get; }
    string Protocol { get; }
    DateTime? CreationTime { get; }
    uint OwningPid { get; }
    ConnectionStatus State { get; }
    bool IsLoopback { get; }

    ITcpRow ToTcpRow();

}