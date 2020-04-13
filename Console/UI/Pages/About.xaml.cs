using System;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
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

        private void OpenExernalLink(object sender, RequestNavigateEventArgs e)
        {
            var src = sender as Hyperlink;
            ProcessHelper.StartShellExecutable(e.Uri.ToString(), null, true);
            e.Handled = true;
        }
    }
}
