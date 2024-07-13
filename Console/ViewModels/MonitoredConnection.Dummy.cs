using Wokhan.WindowsFirewallNotifier.Common.Net.IP;

namespace Wokhan.WindowsFirewallNotifier.Console.ViewModels;

public class ConnectionDummy : MonitoredConnection
{
    public new string Owner { get => "Demo"; private set { } }

    //public ConnectionDummy() { }//: base(new MIB_TCPROW_OWNER_MODULE()) { }

    public ConnectionDummy(Connection ownerMod) : base(ownerMod, null)
    {
    }
}
