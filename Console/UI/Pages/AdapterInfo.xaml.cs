using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Wokhan.WindowsFirewallNotifier.Console.ViewModels;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages;

public partial class AdapterInfo : TimerBasedPage
{

    private List<ExposedInterfaceView> interfacesCollection = NetworkInterface.GetAllNetworkInterfaces().Select(n => new ExposedInterfaceView(n)).OrderByDescending(n => n.Information.OperationalStatus.ToString()).ToList();

    public IEnumerable<ExposedInterfaceView> AllInterfaces => interfacesCollection;

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
