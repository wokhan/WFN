using CommunityToolkit.Mvvm.ComponentModel;

using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;

using Wokhan.ComponentModel.Extensions;
using Wokhan.WindowsFirewallNotifier.Common.IO.Files;
using Wokhan.WindowsFirewallNotifier.Common.Logging;
using Wokhan.WindowsFirewallNotifier.Common.Net.DNS;

namespace Wokhan.WindowsFirewallNotifier.Common.UI.ViewModels;

public abstract class ConnectionBaseInfo : ObservableObject
{
    public DateTime CreationTime { get; init; }

    public uint Pid { get; protected set; }

    public string? IconPath { get; protected set; }

    protected BitmapSource? _icon;
    public BitmapSource? Icon
    {
        get => this.GetOrSetValueAsync(() => IconHelper.GetIconAsync(IconPath ?? Path), ref _icon, OnPropertyChanged);
        set => this.SetValue(ref _icon, value, OnPropertyChanged);
    }

    private string? _targetHostName;
    public string? TargetHostName
    {
        get => this.GetOrSetValueAsync(() => ResolvedIPInformation.ResolveIpAddressAsync(TargetIP), ref _targetHostName, OnPropertyChanged);
        protected set => this.SetValue(ref _targetHostName, value, OnPropertyChanged);
    }

    public string? Path { get; protected set; }
    public string? FileName { get; protected set; }
    public string? Description { get; protected set; }
    public string? ProductName { get; protected set; }
    public string? Company { get; protected set; }
    public string? ServiceName { get; protected set; }
    public string? ServiceDisplayName { get; protected set; }
    public string? SourceIP { get; protected set; }
    public string? SourcePort { get; set; }
    public string? TargetIP { get; protected set; }
    public string? TargetPort { get; protected set; }


    public int RawProtocol { get; protected set; }
    public string? Protocol { get; protected set; }
    public string? Direction { get; protected set; }

    protected void SetProductInfo()
    {
        // Setting default values
        Description = Path;
        ProductName = "?";
        Company = "?";

        if (Path is null or "-")
        {
            return;
        }
        else if (Path == "System")
        {
            Description = "System";
            ProductName = "System";
            Company = String.Empty;
        }
        // TODO: To check if stil applies => File.Exists returns false when accessing system32 files from a x86 application; solution would be to target AnyCPU
        else if (File.Exists(Path))
        {
            try
            {
                var fileinfo = FileVersionInfo.GetVersionInfo(Path);

                ProductName = fileinfo.ProductName ?? String.Empty;
                Company = fileinfo.CompanyName ?? String.Empty;

                if (string.IsNullOrWhiteSpace(fileinfo.FileDescription))
                {
                    Description = FileName;
                }
                else
                {
                    Description = fileinfo.FileDescription;
                }
            }
            catch (Exception exc)
            {
                LogHelper.Error("Unable to check the file description.", exc);
                Description = Path;
                ProductName = "?";
                Company = "?";
            }
        }
    }
}
