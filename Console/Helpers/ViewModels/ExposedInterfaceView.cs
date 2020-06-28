using System;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using Wokhan.WindowsFirewallNotifier.Common.Core.Resources;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels
{
    public partial class ExposedInterfaceView : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string MAC => String.Join(":", Information.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2")));

        public NetworkInterface Information { get; private set; }
        public string FormattedBytesSent => ResourcesLoader.FormatBytes(Statistics.BytesSent);
        public string FormattedBytesReceived => ResourcesLoader.FormatBytes(Statistics.BytesReceived);


        public IPInterfaceStatistics Statistics => Information.GetIPStatistics();

        public IPInterfaceProperties Properties => Information.GetIPProperties();

        public ExposedInterfaceView(NetworkInterface inter)
        {
            this.Information = inter;
        }

        internal ExposedInterfaceView()
        {

        }

        internal void UpdateInner(NetworkInterface inter)
        {
            this.Information = inter;
            NotifyPropertyChanged(nameof(Information));
            NotifyPropertyChanged(nameof(Statistics));
            NotifyPropertyChanged(nameof(Properties));
            NotifyPropertyChanged(nameof(MAC));
            NotifyPropertyChanged(nameof(FormattedBytesSent));
            NotifyPropertyChanged(nameof(FormattedBytesReceived));
        }
    }
}
