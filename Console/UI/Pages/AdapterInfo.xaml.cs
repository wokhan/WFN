using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows.Controls;
using System.Windows.Threading;
using Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Interaction logic for AdapterInfo.xaml
    /// </summary>
    public partial class AdapterInfo : Page, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public bool IsTrackingEnabled
        {
            get { return timer.IsEnabled; }
            set { timer.IsEnabled = value; }
        }

        public List<double> Intervals { get { return new List<double> { 0.5, 1, 5, 10 }; } }

        private DispatcherTimer timer = new DispatcherTimer();

        private double _interval = 1;
        public double Interval
        {
            get { return _interval; }
            set { _interval = value; timer.Interval = TimeSpan.FromSeconds(value); }
        }

        private List<ExposedInterfaceView> interfacesCollection = NetworkInterface.GetAllNetworkInterfaces().Select(n => new ExposedInterfaceView(n)).OrderByDescending(n => n.Information.OperationalStatus.ToString()).ToList();

        public IEnumerable<ExposedInterfaceView> AllInterfaces { get { return interfacesCollection; } }
        
        public AdapterInfo()
        {
            this.Loaded += AdapterInfo_Loaded;
            this.Unloaded += AdapterInfo_Unloaded;

            InitializeComponent();
            
            timer.Tick += Timer_Tick;
            timer.Interval = TimeSpan.FromSeconds(Interval);
        }

        private void AdapterInfo_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            timer.Start();
        }

        private void AdapterInfo_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            timer.Stop();
        }

        private void Timer_Tick(object sender, EventArgs e)
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
