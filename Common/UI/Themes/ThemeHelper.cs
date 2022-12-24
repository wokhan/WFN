using Microsoft.Win32;

using System;
using System.Linq;
using System.Windows;

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
    }
}
