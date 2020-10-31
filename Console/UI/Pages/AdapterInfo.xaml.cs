using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using Wokhan.WindowsFirewallNotifier.Console.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Interaction logic for AdapterInfo.xaml
    /// </summary>
    public partial class AdapterInfo : TimerBasedPage, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private static bool trackingState = true;

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private List<ExposedInterfaceView> interfacesCollection = NetworkInterface.GetAllNetworkInterfaces().Select(n => new ExposedInterfaceView(n)).OrderByDescending(n => n.Information.OperationalStatus.ToString()).ToList();

        public IEnumerable<ExposedInterfaceView> AllInterfaces { get { return interfacesCollection; } }
        
        public AdapterInfo()
        {
            InitializeComponent();
        }

        protected override async Task OnTimerTick(object sender, EventArgs e)
        {
            var allnet = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var i in allnet)
            {
                var existing = interfacesCollection.SingleOrDefault(c => c.Information.Id == i.Id);
                if (existing != null)
                {
                    existing.UpdateInner(i);
                }
            }
        }
    }
}
