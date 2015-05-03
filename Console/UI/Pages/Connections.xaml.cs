using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WFNConsole.Helpers;
using WindowsFirewallNotifier;

namespace WFNConsole.UI.Pages
{
    /// <summary>
    /// Interaction logic for Connections.xaml
    /// </summary>
    public partial class Connections : Page
    {
        public class ConnectionView : NotifierHelper
        {
            public ConnectionView(int pid)
            {
                PID = pid;
                IsNew = true;
                LastSeen = DateTime.Now;

                if (pid != 0 && pid != 4)
                {
                    using (Process proc = Process.GetProcessById(pid))
                    {
                        ProcName = proc.ProcessName;
                        try
                        {
                            Path = proc.MainModule.FileName;
                            Icon = ProcessHelper.GetIcon(Path).AsImageSource();
                        }
                        catch
                        {
                            Path = "Unresolved";
                        }
                    }
                }
                else
                {
                    Path = "System";
                    ProcName = "System";
                }
            }

            public string GroupKey { get { return String.Format("{0} ({1}) - [{2}]", ProcName, Path, PID); } }
            public ImageSource Icon { get; set; }
            public long PID { get; set; }
            public string ProcName { get; set; }
            public string Path { get; set; }

            private string _protocol;
            public string Protocol
            {
                get { return _protocol; }
                set { _protocol = value; NotifyPropertyChanged("Protocol"); }
            }

            private string _state;
            public string State
            {
                get { return _state; }
                set { _state = value; NotifyPropertyChanged("State"); }
            }

            private string _localAddress;
            public string LocalAddress
            {
                get { return _localAddress; }
                set { _localAddress = value; NotifyPropertyChanged("LocalAddress"); }
            }

            private string _localPort;
            public string LocalPort
            {
                get { return _localPort; }
                set { _localPort = value; NotifyPropertyChanged("LocalPort"); }
            }

            private string _remoteAddress;
            public string RemoteAddress
            {
                get { return _remoteAddress; }
                set { _remoteAddress = value; NotifyPropertyChanged("RemoteAddress"); }
            }

            private string _remotePort;
            public string RemotePort
            {
                get { return _remotePort; }
                set { _remotePort = value; NotifyPropertyChanged("RemotePort"); }
            }

            public string Owner { get; set; }
            public DateTime CreationTime { get; set; }

            private DateTime _lastSeen;
            public DateTime LastSeen
            {
                get { return _lastSeen; }
                set { _lastSeen = value; NotifyPropertyChanged("RemotePort"); }
            }

            private bool _isDying;
            public bool IsDying
            {
                get { return _isDying; }
                set { _isDying = value; NotifyPropertyChanged("IsDying"); }
            }

            private bool _isNew;
            public bool IsNew
            {
                get { return _isNew; }
                set { _isNew = value; NotifyPropertyChanged("IsNew"); }
            }
        }

        public List<int> Intervals { get { return new List<int> { 1, 5, 10 }; } }

        private DispatcherTimer timer = new DispatcherTimer();

        public ObservableCollection<ConnectionView> lstConnections = new ObservableCollection<ConnectionView>();

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

            this.DataContext = this;
            
            timer.Interval = TimeSpan.FromSeconds(Interval);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            foreach (var b in IpHlpApiHelper.GetAllTCPConnections())
            {
                AddOrUpdateConnection(b, "TCP");
            }

            foreach (var b in IpHlpApiHelper.GetAllUDPConnections())
            {
                AddOrUpdateConnection(b, "UDP");
            }

            if (Socket.OSSupportsIPv6)
            {
                foreach (var b in IpHlpApiHelper.GetAllTCP6Connections())
                {
                    AddOrUpdateConnection(b, "TCP");
                }

                foreach (var b in IpHlpApiHelper.GetAllUDP6Connections())
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

        private void AddOrUpdateConnection(IpHlpApiHelper.OWNER_MODULE b, string protocol)
        {
            ConnectionView lvi = lstConnections.SingleOrDefault(l => l.PID == b.OwningPid && l.Protocol == protocol && l.LocalPort == b.LocalPort.ToString());

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
                lvi = new ConnectionView((int)b.OwningPid)
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
            lvi.State = Enum.GetName(typeof(IpHlpApiHelper.MIB_TCP_STATE), b.State);
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            timer.Start();
            btnStart.IsEnabled = false;
            btnStop.IsEnabled = true;
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
        }

    }
}
