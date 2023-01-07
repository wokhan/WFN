﻿using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Wokhan.WindowsFirewallNotifier.Common.Logging;
using Wokhan.WindowsFirewallNotifier.Console.UI.Pages;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private const uint ERROR_PRIVILEGE_NOT_HELD = 1314;

    /// <summary>
    /// The title displayed in the window incl. version information from assembly.
    /// </summary>
    /// <remarks>
    /// The version after the major/minor number is incremented by an expression in the <![CDATA[<Vesion>,<FileVersion>]]> e.g: 2.1.*.* - see Console.csproj file
    /// </remarks>
    public static string MainWindowTitle
    {
        get
        {
            AssemblyName ai = Assembly.GetExecutingAssembly().GetName();
            return $"{ai.Name} BETA - Version: {ai.Version} {ai.ProcessorArchitecture} {ai.CultureName}";
        }
    }

    public MainWindow()
    {
        InitializeComponent();

        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
    }

    void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        if (e.Exception is InvalidOperationException && e.Exception.InnerException != null && e.Exception.InnerException is Win32Exception && ((Win32Exception)e.Exception.InnerException).NativeErrorCode == ERROR_PRIVILEGE_NOT_HELD)
        {
            ForceDialog("You must run the Windows Firewall Notifier console as an administrator to be able to use this feature.", "Insufficient privileges");
        }
        else
        {
            LogHelper.Error("Unexpected error", e.Exception);
            ForceDialog(e.Exception.Message, "Unexpected error");
        }
        e.Handled = true;
    }

    private async void ForceDialog(string p1, string p2)
    {
        //MessageBox.Show(p1, p2);
        /*var dial = await this.GetCurrentDialogAsync<BaseMetroDialog>();

        if (dial != null)
        {
            dial.Title = p2;
            dial.Content = p1;
            await this.HideMetroDialogAsync(dial);
        }
        else
        {
            await this.ShowMessageAsync(p2, p1, MessageDialogStyle.Affirmative);
        }*/
    }

    void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        LogHelper.Error("Unexpected error", (Exception)e.ExceptionObject);
        ForceDialog(((Exception)e.ExceptionObject).Message, "Unexpected error");
        //this.ShowMessageAsync("Unexpected error", ((Exception)e.ExceptionObject).Message, MessageDialogStyle.Affirmative);
    }

    private void btnRestartAdmin_Click(object sender, RoutedEventArgs e)
    {
        ((App)Application.Current).RestartAsAdmin();
    }

    Options optionWindow = null;
    private void OpenOptions(object sender, RoutedEventArgs e)
    {
        (optionWindow ??= new Options() { Owner = this }).Show();
        optionWindow.Closed += (_, _) => optionWindow = null;
    }

    About aboutWindow = null;
    private void OpenAbout(object sender, RoutedEventArgs e)
    {
        (aboutWindow ??= new About() { Owner = this }).Show();
        aboutWindow.Closed += (_, _) => aboutWindow = null;
    }
}