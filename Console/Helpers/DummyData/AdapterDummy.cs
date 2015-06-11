using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.DummyData
{
    public class AdapterDummy
    {
        public IEnumerable<ExposedInterfaceView> AllInterfaces { get { return NetworkInterface.GetAllNetworkInterfaces().Select(n => new ExposedInterfaceView(n)).OrderByDescending(n => n.Information.OperationalStatus.ToString()).ToList(); } }
    }
}
