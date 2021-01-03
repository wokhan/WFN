using System.Windows;
using System.Windows.Controls;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Controls
{
    public partial class AdminPanel : UserControl
    {
        public string Caption
        {
            get => (string)GetValue(CaptionProperty);
            set => SetValue(CaptionProperty, value);
        }

        public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register(nameof(Caption), typeof(string), typeof(AdminPanel));

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
