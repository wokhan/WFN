using System;
using NetFwTypeLib;
using Wokhan.WindowsFirewallNotifier.Common.Core.Resources;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.WFP.Rules
{
    public class FwRule : Rule
    {
        private INetFwRule InnerRule;

        public FwRule(INetFwRule innerRule)
        {
            InnerRule = innerRule;
        }

        public override NET_FW_ACTION_ Action => InnerRule.Action;

        private string? _applicationName = null;
        public override string ApplicationName
        {
            get
            {
                if (_applicationName is null)
                {
                    _applicationName = InnerRule.ApplicationName != null ? Environment.ExpandEnvironmentVariables(InnerRule.ApplicationName) : string.Empty;

                }
                return _applicationName;
            }
        }
        public override string ApplicationShortName => ApplicationName != null ? System.IO.Path.GetFileName(ApplicationName) : string.Empty;

        public override string? AppPkgId => (InnerRule as INetFwRule3)?.LocalAppPackageId ?? string.Empty;

        private string? _description = null;
        public override string? Description => _description ?? (_description = ResourcesLoader.GetMSResourceString(InnerRule.Description));

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
        public override string Name => _name ?? (_name = ResourcesLoader.GetMSResourceString(InnerRule.Name));

        public override int Profiles => InnerRule.Profiles;
        public override int Protocol => InnerRule.Protocol;
        public override string? RemoteAddresses => InnerRule.RemoteAddresses;
        public override string RemotePorts => InnerRule.RemotePorts;
        public override string? ServiceName => InnerRule.serviceName;

        public override INetFwRule GetPreparedRule(bool isTemp) => InnerRule;
    }
}
