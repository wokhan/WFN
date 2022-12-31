using Microsoft.Win32;

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Wokhan.WindowsFirewallNotifier.Common.UI.Themes
{
    public static partial class ThemeHelper
    {
        public static string GetCurrentTheme()
        {
            if (SystemParameters.HighContrast)
            {
                return "System";
            }

            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize"))
            {
                return (int?)key?.GetValue("AppsUseLightTheme") == 0 ? "Dark" : "Light";
            }
        }
        public static string GetURIForCurrentTheme()
        {
            return $"pack://application:,,,/Wokhan.WindowsFirewallNotifier.Common;component/UI/Themes/{GetCurrentTheme()}.xaml";
        }
    }
}
