using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wokhan.WindowsFirewallNotifier.Common;
using System.Linq;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Options : Page
    {
        private Dictionary<string, Brush> _colors = typeof(Colors).GetProperties(BindingFlags.Public | BindingFlags.Static).ToDictionary(c => c.Name, c => (Brush)new SolidColorBrush((Color)c.GetValue(null)));
        public Dictionary<string, Brush> Colors { get { return _colors; } }

        public SolidColorBrush AccentColor
        {
            get { return (SolidColorBrush)Application.Current.Resources["AccentColorBrush"]; }
            set { Application.Current.Resources["AccentColorBrush"] = value; Settings.Default.AccentColor = value; }
        }

        public Options()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Save();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Reload();
        }

        private void btnTestNotif_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Notifier.exe"));
        }

        private void btnRestartAdmin_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).RestartAsAdmin();
        }
    }
}
