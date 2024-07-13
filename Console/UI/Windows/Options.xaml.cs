using CommunityToolkit.Mvvm.Input;

using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

using Wokhan.WindowsFirewallNotifier.Common.Config;
using Wokhan.WindowsFirewallNotifier.Common.Logging;
using Wokhan.WindowsFirewallNotifier.Common.Processes;
using Wokhan.WindowsFirewallNotifier.Common.UI.Themes;
using Wokhan.WindowsFirewallNotifier.Console.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Windows;

/// <summary>
/// Interaction logic for Settings.xaml
/// </summary>
public partial class Options : Window
{
    public Dictionary<string, Brush> Colors { get; } = typeof(Colors).GetProperties(BindingFlags.Public | BindingFlags.Static).ToDictionary(c => c.Name, c => (Brush)new SolidColorBrush((Color)c.GetValue(null)));

    public Options()
    {
        InitializeComponent();
    }

    [RelayCommand]
    private void OK()
    {
        Settings.Default.FirstRun = true;   // reset the flag to log os info again once
        Settings.Default.Save();
        InstallHelper.SetAuditPolConnection(enableSuccess: Settings.Default.AuditPolEnableSuccessEvent, enableFailure: true);  // always turn this on for now so that security log and notifier works
        Close();
    }

    [RelayCommand]
    private void Cancel()
    {
        Settings.Default.Reload();
        Close();
    }

    [RelayCommand]
    private void TestNotif()
    {
        Process.Start(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProcessNames.Notifier.FileName));
    }

    private void txtCurrentLogPath_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ProcessHelper.OpenFolder(LogHelper.CurrentLogsPath);
    }

    [RelayCommand]
    private void ResetDefault()
    {
        Settings.Default.Reset();
        Settings.Default.FirstRun = true;
        Settings.Default.EnableVerboseLogging = false;
    }

    private void txtUserConfigurationPath_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ProcessHelper.BrowseToFile($"\"{Settings.Default.ConfigurationPath}\"");
    }

    private void SelectTheme(object sender, RoutedEventArgs e)
    {
        Settings.Default.Theme = (string)((ToggleButton)sender).CommandParameter;
    }

    private void ApplyButtonTheme(object sender, RoutedEventArgs e)
    {
        var button = (ToggleButton)sender;
        var theme = (string)button.CommandParameter;

        button.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri(ThemeHelper.GetURIForTheme(theme)) });
    }

    private void SelectTheme(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {

    }
}
