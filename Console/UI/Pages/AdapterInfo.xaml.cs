using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Timers;

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

    protected override void OnTimerTick(object? state, ElapsedEventArgs e)
    {
        var allnet = NetworkInterface.GetAllNetworkInterfaces();
        foreach (var i in allnet)
        {
            var existing = interfacesCollection.SingleOrDefault(c => c.Information.Id == i.Id);
            existing?.UpdateInner(i);
        }
    }
}
