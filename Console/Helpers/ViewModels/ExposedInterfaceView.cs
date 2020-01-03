using System;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels
{
    public class ExposedInterfaceView : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private NetworkInterface _interface;

        public string MAC { get { return String.Join(":", _interface.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2"))); } }

        public NetworkInterface Information { get { return _interface; } }

        public string FormattedBytesSent { get { return CommonHelper.FormatBytes(Statistics.BytesSent); } }
        public string FormattedBytesReceived { get { return CommonHelper.FormatBytes(Statistics.BytesReceived); } }


        public IPInterfaceStatistics Statistics { get { return _interface.GetIPStatistics(); } }

        public IPInterfaceProperties Properties { get { return _interface.GetIPProperties(); } }

        public ExposedInterfaceView(NetworkInterface inter)
        {
            this._interface = inter;
        }

        internal void UpdateInner(NetworkInterface inter)
        {
            this._interface = inter;
            NotifyPropertyChanged(nameof(Information));
            NotifyPropertyChanged(nameof(Statistics));
            NotifyPropertyChanged(nameof(Properties));
            NotifyPropertyChanged(nameof(MAC));
            NotifyPropertyChanged(nameof(FormattedBytesSent));
            NotifyPropertyChanged(nameof(FormattedBytesReceived));
        }
    }
}
