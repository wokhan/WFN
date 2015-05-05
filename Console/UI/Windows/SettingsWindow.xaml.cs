using MahApps.Metro.Controls.Dialogs;
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
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void callback(string title, string details)
        {
            this.ShowMessageAsync(title, details);
        }
        
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (false)
            {
                if (rbEnable.IsChecked.Value)
                {
                    InstallHelper.EnableProgram(Settings.Default.EnableFor == 1);
                }
                else if (InstallHelper.IsInstalled())
                {
                    InstallHelper.RemoveProgram(chkReallowOut.IsChecked.Value, chkDisableLog.IsChecked.Value, callback, callback);
                }
            } 
            
            Settings.Default.Save();
        }
    }
}
