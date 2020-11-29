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
using Wokhan.WindowsFirewallNotifier.Common.Core;

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

        protected ImageSource? _icon;
        public ImageSource? Icon
        {
            get => this.GetOrSetValueAsync(() => IconHelper.GetIconAsync(IconPath ?? Path), NotifyPropertyChanged, nameof(_icon));
            set => this.SetValue(ref _icon, value, NotifyPropertyChanged);
        }

        public string? Path { get; protected set; }
        public string? FileName { get; protected set; }


        protected string? description;
        public string? Description
        {
            get => this.GetOrSetAsyncValue(() => SetFileInformation(nameof(description)), NotifyPropertyChanged, backingFieldName: nameof(description));
            protected set => this.SetValue(ref description, value, NotifyPropertyChanged);
        }

        protected string? productName;
        public string? ProductName
        {
            get => this.GetOrSetAsyncValue(() => SetFileInformation(nameof(productName)), NotifyPropertyChanged, backingFieldName: nameof(productName));
            protected set => this.SetValue(ref productName, value, NotifyPropertyChanged);
        }

        protected string? company;
        public string? Company
        {
            get => this.GetOrSetAsyncValue(() => SetFileInformation(nameof(company)), NotifyPropertyChanged, backingFieldName: nameof(company));
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

        private string? SetFileInformation(string fieldName)
        {
            if (fileInfoResolutionTriggered)
            {
                return null;
            }

            fileInfoResolutionTriggered = true;

            if (Path == "System")
            {
                description = "System";
                productName = String.Empty;
                company = String.Empty;
            }
            else if (File.Exists(Path))
            {
                try
                {
                    var fileinfo = FileVersionInfo.GetVersionInfo(Path);

                    productName = fileinfo.ProductName;
                    company = fileinfo.CompanyName;

                    if (string.IsNullOrWhiteSpace(fileinfo.FileDescription))
                    {
                        description = FileName;
                    }
                    else
                    {
                        description = fileinfo.FileDescription;
                    }
                }
                catch (Exception exc)
                {
                    LogHelper.Error("Unable to check the file description.", exc);
                    description = Path;
                    productName = "?";
                    company = "?";
                }
            }
            else
            {
                // TODO: this happens when accessing system32 files from a x86 application i.e. File.Exists always returns false; solution would be to target AnyCPU
                description = Path;
                productName = "?";
                company = "?";
            }

            switch (fieldName)
            {
                case nameof(description):
                    return description;

                case nameof(productName):
                    return productName;

                case nameof(company):
                    return company;

                default:
                    return String.Empty;
            }
        }
    }
}
