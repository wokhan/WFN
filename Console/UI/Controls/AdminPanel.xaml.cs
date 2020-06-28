using System.Windows;
using System.Windows.Controls;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Controls
{
    /// <summary>
    /// Logique d'interaction pour AdminPanel.xaml
    /// </summary>
    public partial class AdminPanel : UserControl
    {
        public string Caption { get; set; }

        public AdminPanel()
        {
            this.DataContext = this;

            InitializeComponent();
        }
        private void btnRestartAdmin_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).RestartAsAdmin();
        }

    }
}
