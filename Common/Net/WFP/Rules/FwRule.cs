
using NetFwTypeLib;

using System;
using System.IO;
using System.Threading.Tasks;

using System.Windows.Media.Imaging;

using Wokhan.ComponentModel.Extensions;
using Wokhan.WindowsFirewallNotifier.Common.Core.Resources;
using Wokhan.WindowsFirewallNotifier.Common.UAP;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.WFP.Rules;

public class FwRule : Rule
{
    private INetFwRule InnerRule;

    public FwRule(INetFwRule innerRule)
    {
        InnerRule = innerRule;
        IsStoreApp = innerRule is INetFwRule3 { LocalAppPackageId: not null };
    }

    public override NET_FW_ACTION_ Action => InnerRule.Action;

    private string? _applicationName = null;
    public override string? ApplicationName => this.GetOrSetValueAsync(() => SetAppNameAndLogoAsync(), ref _applicationName, OnAppNamePropertyChanged);
    
    private void OnAppNamePropertyChanged(string _)
    {
        OnPropertyChanged(nameof(ApplicationName));
        OnPropertyChanged(nameof(ApplicationShortName));
    }

    public override BitmapSource? Icon => IsStoreApp ? _icon : base.Icon;

    public override string? AppPkgId => (InnerRule as INetFwRule3)?.LocalAppPackageId ?? string.Empty;

    private string? _description = null;
    public override string? Description => _description ??= ResourcesLoader.GetMSResourceString(InnerRule.Description);

    public override NET_FW_RULE_DIRECTION_ Direction => InnerRule.Direction;
    public override bool EdgeTraversal => InnerRule.EdgeTraversal;

    //FIXME: https://msdn.microsoft.com/en-us/library/windows/desktop/dd607258(v=vs.85).aspx   Proper default value...?
    public override int EdgeTraversalOptions => (InnerRule as INetFwRule2)?.EdgeTraversalOptions ?? 0;

    public override bool Enabled => InnerRule.Enabled;
    public override string? Grouping => InnerRule.Grouping;
    public override string? IcmpTypesAndCodes => InnerRule.IcmpTypesAndCodes;
    public override object? Interfaces => InnerRule.Interfaces;
    public override string? InterfaceTypes => InnerRule.InterfaceTypes;
    public override string? LocalAddresses => InnerRule.LocalAddresses;
    public override string LocalPorts => InnerRule.LocalPorts;
    public override string? LUOwn => (InnerRule as INetFwRule3)?.LocalUserOwner ?? string.Empty;

    private string? _name = null;
    public override string Name => _name ??= ResourcesLoader.GetMSResourceString(InnerRule.Name);

    public override int Profiles => InnerRule.Profiles;
    public override int Protocol => InnerRule.Protocol;
    public override string? RemoteAddresses => InnerRule.RemoteAddresses;
    public override string RemotePorts => InnerRule.RemotePorts;
    public override string? ServiceName => InnerRule.serviceName;
    public override bool IsStoreApp { get; }
    public override INetFwRule GetPreparedRule(bool isTemp) => InnerRule;

    private async Task<string?> SetAppNameAndLogoAsync()
    {
        if (InnerRule.ApplicationName is not null)
        {
            return Environment.ExpandEnvironmentVariables(InnerRule.ApplicationName);
        }
        // Syntax is weird. "(netFwRule as INetFwRule3)?.LocalAppPackageId is not null" looks more readable, doesn't it? ;-)
        else if (IsStoreApp)
        {
            // Parsing the package DisplayName ressource path (something along "@{PackageName_PackageId/ms:resources://DisplayName}")
            var packageName = InnerRule.Name.Split('?')[0][2..];
            var res = await StorePackageHelper.GetPackageBasicInfoAsync(packageName);

            if (res.LogoPath is not null)
            {
                Icon = new BitmapImage(new Uri(res.LogoPath));
            }

            if (res.Executable is not null)
            {
                return Path.Combine(res.RootFolder!, res.Executable);
            }
        }

        return string.Empty;
    }
}
