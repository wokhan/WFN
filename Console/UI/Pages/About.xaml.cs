using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Logique d'interaction pour About.xaml
    /// </summary>
    public partial class About : Page
    {
        public Version Version { get { return Assembly.GetExecutingAssembly().GetName().Version; } }
        public string ProductName { get { return Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product; } }

        public About()
        {
            InitializeComponent();
        }

        private void btnDonate_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=wokhan%40online%2efr&lc=US&item_name=Khan%20%28Windows%20Firewall%20Notifier%29&item_number=WOK%2dWFN&currency_code=EUR&bn=PP%2dDonationsBF%3abtn_donateCC_LG%2egif%3aNonHosted";
            ProcessHelper.StartShellExecutable(url, null, true);
        }
    }
}
