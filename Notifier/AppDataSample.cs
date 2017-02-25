using System;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using Wokhan.WindowsFirewallNotifier.Notifier.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Notifier
{
    public class AppDataSample
    {
        private ObservableCollection<CurrentConn> _conns = new ObservableCollection<CurrentConn>();
        public ObservableCollection<CurrentConn> Connections { get { return _conns; } }

        public AppDataSample()
        {
            Connections.Add(new Helpers.CurrentConn() { CurrentPath = "Test 1", CurrentAppPkgId = String.Empty, CurrentService = "Service", CurrentProd = "Sample data", Editor = "Wokhan Solutions", Icon = new BitmapImage(new Uri("/Notifier;component/Resources/WFN Logo.png", UriKind.Relative)) });
        }
    }
}
