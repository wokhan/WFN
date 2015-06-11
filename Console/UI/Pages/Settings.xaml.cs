using System.Windows;
using System.Windows.Controls;
using Wokhan.WindowsFirewallNotifier.Common;
using Wokhan.WindowsFirewallNotifier.Console.Helpers;
using Wokhan.WindowsFirewallNotifier.Console.UI.Windows;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        public bool IsAdmin { get { return ((App)Application.Current).IsElevated; } }

        public bool IsInstalled { get { return InstallHelper.IsInstalled(); } }

        private void callback(string title, string details)
        {
            MessageBox.Show(details, title);
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            btnOK.IsEnabled = false;
            btnCancel.IsEnabled = false;

            if (!IsInstalled && rbEnable.IsChecked.Value)
            {
                InstallHelper.EnableProgram(Settings.Default.EnableFor == 1);
            }
            else if (IsInstalled && rbDisable.IsChecked.Value)
            {
                InstallHelper.RemoveProgram(chkReallowOut.IsChecked.Value, chkDisableLog.IsChecked.Value, callback, callback);
            }

            Settings.Default.Save();

            ((MainWindow)Window.GetWindow(this)).GoBack();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Window.GetWindow(this)).GoBack();
        }

        private void btnRestartAdmin_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).RestartAsAdmin();
        }
    }
}
