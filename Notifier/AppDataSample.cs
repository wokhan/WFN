using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using Wokhan.WindowsFirewallNotifier.Notifier.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Notifier
{
    public class AppDataSample
    {
        internal static CurrentConn DemoConnection = new CurrentConn() { CurrentPath = "Test 1", CurrentAppPkgId = String.Empty, CurrentService = "Service", Description = "Sample data", ProductName = "WFN", Company = "Wokhan Solutions", Icon = new BitmapImage(new Uri("pack://application:,,,/Wokhan.WindowsFirewallNotifier.Common;component/Resources/Shield.ico")) };
        public IList<CurrentConn> Connections { get; } = new List<CurrentConn> { DemoConnection, DemoConnection };
    }
}
