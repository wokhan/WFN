using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

using Wokhan.WindowsFirewallNotifier.Common.Config;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Common.Logging;
using Wokhan.WindowsFirewallNotifier.Common.Processes;
using Wokhan.WindowsFirewallNotifier.Common.UI.Themes;
using Wokhan.WindowsFirewallNotifier.Console.UI.Pages;

namespace Wokhan.WindowsFirewallNotifier.Console;

public partial class App : Application
{
    public App() : base()
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        this.DispatcherUnhandledException += Current_DispatcherUnhandledException;

        LogHelper.Debug("Starting Console: " + Environment.CommandLine);

        if (Settings.Default.AlwaysRunAs && !UAC.CheckProcessElevated())
        {
            RestartAsAdmin();
        }

        Settings.Default.PropertyChanged += SettingsChanged;
    }

    private string currentTheme = "Automatic";
    private void SettingsChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.Theme) && currentTheme != Settings.Default.Theme)
        {
            currentTheme = Settings.Default.Theme;
            Resources.MergedDictionaries[0].Source = new Uri(ThemeHelper.GetURIForCurrentTheme());
        }
    }

    private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.Message, Common.Properties.Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        MessageBox.Show(((Exception)e.ExceptionObject).Message, Common.Properties.Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public bool IsElevated { get; } = UAC.CheckProcessElevated();

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        //if (Settings.Default.AccentColor != null)
        //{
        //    Resources["AccentColorBrush"] = Settings.Default.AccentColor;
        //}

        if (Settings.Default.ConsoleSizeWidth > 900)
        {
            Resources["ConsoleSizeWidth"] = Settings.Default.ConsoleSizeWidth;
        }

        if (Settings.Default.ConsoleSizeHeight > 600)
        {
            Resources["ConsoleSizeHeight"] = Settings.Default.ConsoleSizeHeight;
        }
            
        var themeUri = ThemeHelper.GetURIForCurrentTheme();
        Resources.MergedDictionaries[0].Source = new Uri(themeUri);
    }

    internal void RestartAsAdmin()
    {
        if (System.Diagnostics.Debugger.IsAttached)
        {
            MessageBox.Show("WFN is currently being debugged with a non-admin Visual Studio instance.\r\nDebugger will be detached if you want to use admin-only features as it requires launching a new WFN instance.\r\nTo avoid this, please run Visual Studio with admin privileges.", "Non-admin debugger detected", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        ProcessHelper.RunElevated(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ProcessNames.WFN.FileName));
        Environment.Exit(0);
    }
}
