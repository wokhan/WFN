﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Linq;
using Wokhan.WindowsFirewallNotifier.Console.Helpers;
using Wokhan.WindowsFirewallNotifier.Common.Config;
using Wokhan.WindowsFirewallNotifier.Common.Logging;
using Wokhan.WindowsFirewallNotifier.Common.Processes;
using Wokhan.WindowsFirewallNotifier.Common.UI.Themes;
using System.Windows.Controls.Primitives;

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

    private void btnOK_Click(object sender, RoutedEventArgs e)
    {
        Settings.Default.FirstRun = true;   // reset the flag to log os info again once
        Settings.Default.Save();
        InstallHelper.SetAuditPolConnection(enableSuccess: Settings.Default.AuditPolEnableSuccessEvent, enableFailure: true);  // always turn this on for now so that security log and notifier works
        Close();
    }

    private void btnCancel_Click(object sender, RoutedEventArgs e)
    {
        Settings.Default.Reload();
        Close();
    }

    private void Close()
    {
        Window.GetWindow(this).Close();
    }

    private void btnTestNotif_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Notifier.exe"));
    }

    private void txtCurrentLogPath_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ProcessHelper.StartShellExecutable("explorer.exe", LogHelper.CurrentLogsPath, true);
    }

    private void btnResetDefault_Click(object sender, RoutedEventArgs e)
    {
        Settings.Default.Reset();
        Settings.Default.FirstRun = true;
        Settings.Default.EnableVerboseLogging = false;
    }

    private void txtUserConfigurationPath_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ProcessHelper.StartShellExecutable("explorer.exe", $"\"{Settings.Default.ConfigurationPath}\"", true);
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
