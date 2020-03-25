using System;
using NetFwTypeLib;
using System.ComponentModel;
using System.Collections.Generic;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.WFP
{
    public class CustomRule : Rule
    {
        public enum CustomRuleAction
        {
            [Description("Allow")]
            A,
            [Description("Block")]
            B
        }

        public override NET_FW_ACTION_ Action { get; }
        public override string ApplicationName { get; }
        public override string ApplicationShortName { get; }
        public override string AppPkgId { get; }
        public override string Description { get; }
        public override NET_FW_RULE_DIRECTION_ Direction { get; }
        public override bool EdgeTraversal { get; }
        public override int EdgeTraversalOptions { get; }
        public override bool Enabled { get; }
        public override string Grouping { get; }
        public override string IcmpTypesAndCodes { get; }
        public override object Interfaces { get; }
        public override string InterfaceTypes { get; }
        public override string LocalAddresses { get; }
        public override string LocalPorts { get; }
        public override string LUOwn { get; }
        public override string Name { get; }
        public override int Profiles { get; }
        public override int Protocol { get; }
        public override string RemoteAddresses { get; }
        public override string RemotePorts { get; }
        public override string? ServiceName { get; }

        public override INetFwRule GetPreparedRule(bool isTemp)
        {
            INetFwRule firewallRule;

#pragma warning disable CS8600 // Ignore possible null value returned from CreateInstance
#pragma warning disable CS8604 // Ignore possible null argument for CreateInstance
            if (!string.IsNullOrEmpty(AppPkgId))
            {
                //Need INetFwRule3
                firewallRule = (INetFwRule3)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
            }
            else
            {
                firewallRule = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
            }
#pragma warning restore CS8600
#pragma warning restore CS8604
            firewallRule.Action = Action;
            firewallRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
            firewallRule.Enabled = true;
            firewallRule.Profiles = Profiles;
            firewallRule.InterfaceTypes = "All";
            firewallRule.Name = Name;
            firewallRule.ApplicationName = ApplicationName;

            if (!string.IsNullOrEmpty(AppPkgId))
            {
                ((INetFwRule3)firewallRule).LocalAppPackageId = AppPkgId;

                //This needs to be set as well
                ((INetFwRule3)firewallRule).LocalUserOwner = LUOwn;
            }

            if (!string.IsNullOrEmpty(ServiceName))
            {
                firewallRule.serviceName = ServiceName;
            }

            if (Protocol != -1)
            {
                firewallRule.Protocol = (int)normalizeProtocol(Protocol);
            }

            if (!string.IsNullOrEmpty(LocalPorts))
            {
                firewallRule.LocalPorts = LocalPorts;

                if (!isTemp)
                {
                    firewallRule.Name += " [L:" + LocalPorts + "]";
                }
            }

            if (!string.IsNullOrEmpty(RemoteAddresses))
            {
                firewallRule.RemoteAddresses = RemoteAddresses;

                if (!isTemp)
                {
                    firewallRule.Name += " [T:" + RemoteAddresses + "]";
                }
            }

            if (!string.IsNullOrEmpty(RemotePorts))
            {
                firewallRule.RemotePorts = RemotePorts;

                if (!isTemp)
                {
                    firewallRule.Name += " [R:" + RemotePorts + "]";
                }
            }

            return firewallRule;

        }

        public CustomRule(string ruleName, string currentPath, string currentAppPkgId, string localUserOwner, IEnumerable<string> services, int protocol, string target, string targetPort, string localport
            , int profiles, CustomRuleAction action)
            : this(ruleName, currentPath, currentAppPkgId, localUserOwner, string.Join(",", services), protocol, target, targetPort, localport, profiles, action)
        {
            //Chained to the constructor below!
        }

        public CustomRule(string ruleName, string currentPath, string currentAppPkgId, string localUserOwner, string services, int protocol, string target
            , string targetPort, string localport, int profiles, CustomRuleAction action)
        {
            Name = ruleName;
            ApplicationName = currentPath;
            AppPkgId = currentAppPkgId;
            LUOwn = localUserOwner;
            ServiceName = String.IsNullOrEmpty(services) ? null : services;
            Protocol = protocol;
            RemoteAddresses = target;
            RemotePorts = targetPort;
            LocalPorts = localport;
            Profiles = profiles;
            switch (action)
            {
                case CustomRuleAction.A:
                    Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                    break;

                case CustomRuleAction.B:
                    Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
                    break;

                default:
                    throw new Exception("Unknown action type: " + action.ToString());
            }
        }
    }
}