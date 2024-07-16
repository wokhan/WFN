using CommunityToolkit.Mvvm.Input;

using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
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
    public IList<string> Backgrounds { get; } = Enumerable.Range(1, 10).Select(i => $"/WFN;component/Resources/Backgrounds/BG ({i}).jpg").ToList();

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


    [RelayCommand]
    private void SelectTheme(string theme)
    {
        Settings.Default.Theme = theme;
    }

    ToggleButton? selected = null;
    private void SelectBackground(object sender, RoutedEventArgs e)
    {
        var source = (ToggleButton)sender;
        if (selected is not null)
        {
            selected.IsChecked = false;
        }
        selected = source;

        Settings.Default.Background = (string)source.CommandParameter;
    }


    private void ApplyButtonTheme(object sender, RoutedEventArgs e)
    {
        var button = (ToggleButton)sender;
        var theme = (string)button.CommandParameter;

        button.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri(ThemeHelper.GetURIForTheme(theme)) });
    }

    private void ToggleButtonBG_Loaded(object sender, RoutedEventArgs e)
    {
        var source = (ToggleButton)sender;
        if (Settings.Default.Background.Equals(source.CommandParameter))
        {
            source.IsChecked = true;
            selected = source;
        }
    }

    [RelayCommand]
    private void OpenLogLocation()
    {
        ProcessHelper.OpenFolder(LogHelper.CurrentLogsPath);
    }


    [RelayCommand]
    private void OpenSettingsLocation()
    {
        ProcessHelper.BrowseToFile(Settings.Default.ConfigurationPath);
    }
}
