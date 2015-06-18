using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Wokhan.WindowsFirewallNotifier.Notifier.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Notifier.UI.Windows
{
    /// <summary>
    /// Logique d'interaction pour ServicesForm.xaml
    /// </summary>
    public partial class ServicesForm : Window
    {
        private CurrentConn activeConn;

        public class ServiceView
        {
            public bool IsSelected { get; set; }
            public string Name { get; internal set; }
            public string Description { get; internal set; }
        }

        private List<ServiceView> _services;
        public List<ServiceView> Services { get { return _services; } }

        public ServicesForm(CurrentConn activeConn)
        {
            InitializeComponent();

            this.activeConn = activeConn;
            this._services = activeConn.PossibleServices.Select((s, i) => new ServiceView { Name = s, Description = activeConn.PossibleServicesDesc[i], IsSelected = true }).ToList();
        }

        public bool CreateAppRule { get; set; }
        public string[] SelectedServices { get { return Services.Where(s => s.IsSelected).Select(s => s.Name).ToArray(); } }
    }
}
