
using Wokhan.WindowsFirewallNotifier.Common.Net.IP;

namespace Wokhan.WindowsFirewallNotifier.Console.ViewModels
{
    public class ConnectionDummy : Connection
    {
        public new string Owner { get => "Demo"; private set { } }

        public ConnectionDummy() : base(new MIB_TCPROW_OWNER_MODULE()) { }

        public ConnectionDummy(IConnectionOwnerInfo ownerMod) : base(ownerMod)
        {
        }
    }
}
