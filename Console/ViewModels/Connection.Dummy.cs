using System;
using System.Collections.Generic;
using System.Text;

using Wokhan.WindowsFirewallNotifier.Common.Net.IP;

namespace Wokhan.WindowsFirewallNotifier.Console.ViewModels
{
    public class ConnectionDummy : Connection
    {
        public new string Owner { get => "Demo"; private set { } }

        public ConnectionDummy(IConnectionOwnerInfo ownerMod) : base(ownerMod)
        {
        }
    }
}
