using System;
using System.Windows.Media;
using System.ComponentModel;
using Wokhan.WindowsFirewallNotifier.Common.IO.Files;
using Wokhan.WindowsFirewallNotifier.Common.Net.DNS;
using System.Diagnostics;
using Wokhan.WindowsFirewallNotifier.Common.Logging;
using System.IO;
using Wokhan.ComponentModel.Extensions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Wokhan.WindowsFirewallNotifier.Common.UI.ViewModels
{
    public class ConnectionBaseInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public DateTime? CreationTime { get; protected set; }

        public uint Pid { get; protected set; }

        public string? IconPath { get; protected set; }

        private ImageSource? _icon;
        public ImageSource? Icon
        {
            get
            {
                if (_icon is null) UpdateIcon();
                return _icon;
            }
            set => this.SetValue(ref _icon, value, NotifyPropertyChanged);
        }

        private async void UpdateIcon()
        {
            Icon = await IconHelper.GetIconAsync(IconPath ?? Path).ConfigureAwait(false);
        }
                
        public string? Path { get; protected set; }
        public string? FileName { get; protected set; }

        private string? description;
        public string? Description
        {
            get => SetFileInformation(description);
            protected set => this.SetValue(ref description, value, NotifyPropertyChanged);
        }

        private string? productName;
        public string? ProductName
        {
            get => SetFileInformation(productName);
            protected set => this.SetValue(ref productName, value, NotifyPropertyChanged);
        }

        private string? company;
        public string? Company
        {
            get => SetFileInformation(company);
            protected set => this.SetValue(ref company, value, NotifyPropertyChanged);
        }

        public string? ServiceName { get; protected set; }
        public string? SourceIP { get; protected set; }
        public string? SourcePort { get; set; }
        public string? TargetIP { get; protected set; }
        public string? TargetPort { get; protected set; }

        
        private string? _targetHostName;
        public string? TargetHostName
        {
            get
            {
                if (_targetHostName is null)
                {
                    _ = DnsResolver.ResolveIpAddressAsync(TargetIP, entry => TargetHostName = entry.DisplayText);
                }
                return _targetHostName;
            }
            protected set => this.SetValue(ref _targetHostName, value, NotifyPropertyChanged);
        }

        public int RawProtocol { get; protected set; }
        public string? Protocol { get; protected set; }
        public string? Direction { get; protected set; }
        public string? FilterId { get; protected set; }

        private bool fileInfoResolutionTriggered;
        private string? SetFileInformation(string? sourceValue)
        {
            if (sourceValue != null || fileInfoResolutionTriggered)
            {
                return sourceValue;
            }

            fileInfoResolutionTriggered = true;
            
            SetFileInformationInternalAsync();

            return null;
        }

        private async void SetFileInformationInternalAsync()
        {
            if (Path == "System")
            {
                Description = "System";
                ProductName = String.Empty;
                Company = String.Empty;
                return;
            }

            await Task.Run(() =>
            {
                if (File.Exists(Path))
                {
                    try
                    {
                        var fileinfo = FileVersionInfo.GetVersionInfo(Path);
                        
                        ProductName = fileinfo.ProductName;
                        Company = fileinfo.CompanyName;
                        
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
                    }
                }
                else
                {
                    // TODO: this happens when accessing system32 files from a x86 application i.e. File.Exists always returns false; solution would be to target AnyCPU
                    Description = Path;
                    ProductName = "?";
                    Company = "?";
                }
            }).ConfigureAwait(false);
        }
    }

}
