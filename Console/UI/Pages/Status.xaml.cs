using CommunityToolkit.Mvvm.Input;

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

using Wokhan.WindowsFirewallNotifier.Common.Config;
using Wokhan.WindowsFirewallNotifier.Common.Logging;
using Wokhan.WindowsFirewallNotifier.Common.Processes;
using Wokhan.WindowsFirewallNotifier.Console.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages;

public partial class Status : Page
{
    FirewallStatusWrapper status = new FirewallStatusWrapper();

    public ObservableCollection<string> Messages { get; } = new ObservableCollection<string>();

    public Status()
    {
        InitializeComponent();

        this.Loaded += init;
    }

    private void init(object src = null, RoutedEventArgs args = null)
    {
        status = new FirewallStatusWrapper();

        stackOptions.DataContext = status;
        messsageInfoPanel.DataContext = this;
    }

    [RelayCommand]
    private void Save()
    {
        Settings.Default.Save();

        Messages.Add("=== Applying modifications ===");

        status.Save(RunAndLogResult);

        init();
    }

    [RelayCommand]
    private void Revert()
    {
        Messages.Add("=== Reverting modifications ===");

        //TODO: not implemented properly
        Settings.Default.IsInstalled = false;
        Settings.Default.Save();

        init();
    }

    [RelayCommand]
    private void TestNotif()
    {
        // TODO: Does not show if mimimized to tray
        //ProcessHelper.StartOrRestoreToForeground(ProcessNames.Notifier);
        Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProcessNames.Notifier.FileName));
    }

    private bool RunAndLogResult(Func<bool> func, string okMsg, string errorMsg)
    {
        try
        {
            bool success = func();
            var msg = success ? okMsg : errorMsg;
            Messages.Add($"{DateTime.Now} - {msg}");

            LogHelper.Debug($"{func.Method.Name}: {msg}");

            return success;
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex.Message, ex);

            Messages.Add($"{DateTime.Now} - {errorMsg}: {ex.Message}");
            return false;
        }
    }
}
