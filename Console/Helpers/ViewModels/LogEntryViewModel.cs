using System;
using System.Windows.Media;
using Wokhan.WindowsFirewallNotifier.Common;
using System.ComponentModel;
using Wokhan.WindowsFirewallNotifier.Common.IO.Files;
using Wokhan.WindowsFirewallNotifier.Common.Net.DNS;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels
{
    public class LogEntryViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public int Id { get; set; }
        public int Pid { get; set; }
        public DateTime Timestamp { get; set; }


        private ImageSource _icon;
        public ImageSource Icon
        {
            get
            {
                if (_icon is null) UpdateIcon();
                return _icon;
            }
            set
            {
                if (_icon != value)
                {
                    _icon = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));
                }
            }
        }

        private async void UpdateIcon()
        {
            Icon = await IconHelper.GetIconAsync(Path).ConfigureAwait(false);
        }

        public string FileName { get; set; }
        public string Path { get; set; }
        public string FriendlyPath { get; set; }
        public string ServiceName { get; set; }
        public string TargetIP { get; set; }
        private string _targetHostName;
        public string TargetHostName
        {
            get
            {
                if (_targetHostName is null)
                    DnsResolver.ResolveIpAddress(TargetIP, entry => TargetHostName = entry.DisplayText);
                return _targetHostName;
            }
            set
            {
                if (_targetHostName != value)
                {
                    _targetHostName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TargetHostName)));
                }
            }
        }

        public string TargetPort { get; set; }
        public string Protocol { get; set; }
        public string Direction { get; set; }
        public string FilterId { get; set; }

        public string Reason { get; set; }
        public string Reason_Info { get; set; }

        public SolidColorBrush ReasonColor { get; set; }

        public SolidColorBrush DirectionColor { get; set; }

    }

}
