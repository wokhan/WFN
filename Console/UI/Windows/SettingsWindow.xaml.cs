using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using Wokhan.WindowsFirewallNotifier.Common;
using Wokhan.WindowsFirewallNotifier.Console.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Windows
{
    /// <summary>
    /// Interaction logic for SetupWindow.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        public Version Version { get { return Assembly.GetExecutingAssembly().GetName().Version; } }
        public string ProductName { get { return Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product; } }
        
        public bool IsInstalled {  get { return InstallHelper.IsInstalled(); } }

        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void callback(string title, string details)
        {
            MessageBox.Show(details, title);
        }
        
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {

            if (!IsInstalled && rbEnable.IsChecked.Value)
            {
                InstallHelper.EnableProgram(Settings.Default.EnableFor == 1);
            }
            else if (IsInstalled && rbDisable.IsChecked.Value)
            {
                InstallHelper.RemoveProgram(chkReallowOut.IsChecked.Value, chkDisableLog.IsChecked.Value, callback, callback);
            }

            Settings.Default.Save();
        }

        private void btnDonate_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=wokhan%40online%2efr&lc=US&item_name=Khan%20%28Windows%20Firewall%20Notifier%29&item_number=WOK%2dWFN&currency_code=EUR&bn=PP%2dDonationsBF%3abtn_donateCC_LG%2egif%3aNonHosted");
        }
    }
}
