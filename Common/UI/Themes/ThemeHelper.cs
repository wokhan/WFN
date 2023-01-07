using Microsoft.Win32;

using System;
using System.Linq;
using System.Windows;

using Wokhan.WindowsFirewallNotifier.Common.Config;

namespace Wokhan.WindowsFirewallNotifier.Common.UI.Themes;

public static class ThemeHelper
{
    public const string THEME_LIGHT = "Light";
    public const string THEME_DARK = "Dark";
    public const string THEME_SYSTEM = "System";
    public const string THEME_AUTO = "Automatic";

    public static string GetActiveTheme()
    {
        if (Settings.Default.Theme is null or "" or THEME_AUTO)
        {
            if (SystemParameters.HighContrast)
            {
                return THEME_SYSTEM;
            }

            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize"))
            {
                return (int?)key?.GetValue("AppsUseLightTheme") == 0 ? THEME_DARK : THEME_LIGHT;
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
        if (themeName == THEME_AUTO)
        {
            return GetURIForCurrentTheme();
        }

        return $"pack://application:,,,/Wokhan.WindowsFirewallNotifier.Common;component/UI/Themes/{themeName}.xaml";
    }
}
