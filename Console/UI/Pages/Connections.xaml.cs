using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Wokhan.WindowsFirewallNotifier.Console.Helpers;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Common.Helpers.IPHelpers;
using Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Interaction logic for Connections.xaml
    /// </summary>
    public partial class Connections : Page
    {
        public bool IsTrackingEnabled
        {
            get { return timer.IsEnabled; }
            set { timer.IsEnabled = value; }
        }

        public List<int> Intervals { get { return new List<int> { 1, 5, 10 }; } }

        private DispatcherTimer timer = new DispatcherTimer();

        public ObservableCollection<ConnectionViewModel> lstConnections = new ObservableCollection<ConnectionViewModel>();

        public ListCollectionView connectionsView { get; set; }

        private int _interval = 1;
        public int Interval
        {
            get { return _interval; }
            set { _interval = value; timer.Interval = TimeSpan.FromSeconds(value); }
        }

        public Connections()
        {
            connectionsView = new ListCollectionView(lstConnections);
            connectionsView.GroupDescriptions.Add(new PropertyGroupDescription("GroupKey"));

            InitializeComponent();

            timer.Interval = TimeSpan.FromSeconds(Interval);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            foreach (var b in TCPHelper.GetAllTCPConnections())
            {
                AddOrUpdateConnection(b, "TCP");
            }

            foreach (var b in UDPHelper.GetAllUDPConnections())
            {
                AddOrUpdateConnection(b, "UDP");
            }

            if (Socket.OSSupportsIPv6)
            {
                foreach (var b in TCP6Helper.GetAllTCP6Connections())
                {
                    AddOrUpdateConnection(b, "TCP");
                }

                foreach (var b in UDP6Helper.GetAllUDP6Connections())
                {
                    AddOrUpdateConnection(b, "UDP");
                }
            }

            for (int i = lstConnections.Count - 1; i > 0; i--)
            {
                var item = lstConnections[i];
                double elapsed = DateTime.Now.Subtract(item.LastSeen).TotalSeconds;
                if (elapsed > 5)
                {
                    lstConnections.Remove(item);
                }
                else if (elapsed > 2)
                {
                    item.IsDying = false;
                }
            }
        }

        private void AddOrUpdateConnection(BaseHelper.I_OWNER_MODULE b, string protocol)
        {
            ConnectionViewModel lvi = lstConnections.SingleOrDefault(l => l.PID == b.OwningPid && l.Protocol == protocol && l.LocalPort == b.LocalPort.ToString());

            string ownerStr = (b.OwnerModule == null ? String.Empty : b.OwnerModule.ModuleName);

            if (lvi != null)
            {
                if (DateTime.Now.Subtract(lvi.LastSeen).TotalMilliseconds > 500)
                {
                    lvi.IsNew = false;
                }
            }
            else
            {
                lvi = new ConnectionViewModel((int)b.OwningPid)
                {
                    LocalPort = b.LocalPort.ToString(),
                    Owner = ownerStr,
                    CreationTime = b.CreationTime
                };

                lstConnections.Add(lvi);
            }

            lvi.LocalAddress = b.LocalAddress;
            lvi.Protocol = protocol;
            lvi.RemoteAddress = b.RemoteAddress;
            lvi.RemotePort = (b.RemotePort == -1 ? String.Empty : b.RemotePort.ToString());
            lvi.LastSeen = DateTime.Now;
            lvi.State = Enum.GetName(typeof(BaseHelper.MIB_TCP_STATE), b.State);
        }
    }
}