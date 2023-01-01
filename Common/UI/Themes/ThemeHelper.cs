using Microsoft.Win32;

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;

using Wokhan.WindowsFirewallNotifier.Common.Config;

namespace Wokhan.WindowsFirewallNotifier.Common.UI.Themes
{
    public static partial class ThemeHelper
    {
        public static string GetActiveTheme()
        {
            if (Settings.Default.Theme is null or "Automatic")
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
            else
            {
                return Settings.Default.Theme;
            }
        }

        public static string GetURIForCurrentTheme()
        {
            return $"pack://application:,,,/Wokhan.WindowsFirewallNotifier.Common;component/UI/Themes/{GetActiveTheme()}.xaml";
        }

        public static string GetURIForTheme(string themeName)
        {
            if (themeName == "Automatic")
            {
                return GetURIForCurrentTheme();
            }

            return $"pack://application:,,,/Wokhan.WindowsFirewallNotifier.Common;component/UI/Themes/{themeName}.xaml";
        }
    }
}
