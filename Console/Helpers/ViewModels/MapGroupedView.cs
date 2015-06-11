using System.Collections.Generic;
using System.Net;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels
{
    public class MapGroupedView : GroupedViewBase
    {
        public List<IPAddress> Targets { get; set; }
    }
}
