using System;
using NetFwTypeLib;
using System.Linq;
using Microsoft.Win32;
using System.Collections.Generic;
using Wokhan.WindowsFirewallNotifier.Common.Net.WFP.Rules;
using Wokhan.WindowsFirewallNotifier.Common.Logging;
using Wokhan.WindowsFirewallNotifier.Common.Processes;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.WFP
{
    public static partial class FirewallHelper
    {
#pragma warning disable CS8600,CS8601,CS8604 // Ignore possible null value.
        private static INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
#pragma warning restore CS8600,CS8601,CS8604

        public static bool AddRule(INetFwRule rule)
        {
            try
            {
                LogHelper.Debug("Adding rule to firewall...");
                firewallPolicy.Rules.Add(rule);

                return true;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to add the rule to the Windows Firewall", e);
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ruleName"></param>
        /// <returns></returns>
        public static bool RemoveRule(string ruleName)
        {
            try
            {
                firewallPolicy.Rules.Remove(ruleName);
                LogHelper.Debug($"Remove rule: {ruleName} - success.");
                return true;
            }
            catch (UnauthorizedAccessException uae)
            {
                //Don't have enough permissions
                LogHelper.Warning($"{uae.Message}");
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to remove the rule.", e);
            }
            return false;
        }

        public static int GetGlobalProfile()
        {
            return (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL;
        }

        public static bool CheckFirewallEnabled()
        {
            return firewallPolicy.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL] ||
                   firewallPolicy.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE] ||
                   firewallPolicy.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC] ||
                   firewallPolicy.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool EnableWindowsFirewall()
        {
            try
            {
                firewallPolicy.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE] = true;
                firewallPolicy.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC] = true;
                firewallPolicy.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN] = true;

                firewallPolicy.DefaultInboundAction[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE] = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
                firewallPolicy.DefaultInboundAction[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC] = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
                firewallPolicy.DefaultInboundAction[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN] = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;

                firewallPolicy.DefaultOutboundAction[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE] = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
                firewallPolicy.DefaultOutboundAction[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC] = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
                firewallPolicy.DefaultOutboundAction[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN] = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;

                firewallPolicy.NotificationsDisabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE] = false;
                firewallPolicy.NotificationsDisabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC] = false;
                firewallPolicy.NotificationsDisabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN] = false;

                return true;
            }
            catch
            {
                //FIXME: Log the error?
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool RestoreWindowsFirewall()
        {
            try
            {
                firewallPolicy.DefaultInboundAction[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE] = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
                firewallPolicy.DefaultInboundAction[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC] = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
                firewallPolicy.DefaultInboundAction[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN] = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;

                firewallPolicy.DefaultOutboundAction[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE] = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                firewallPolicy.DefaultOutboundAction[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC] = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                firewallPolicy.DefaultOutboundAction[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN] = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;

                return true;
            }
            catch
            {
                //FIXME: Log the error?
            }

            return false;
        }

        private static Rule[]? wshRulesCache = null;

        public static Rule[] GetRules(bool AlsoGetInactive = false)
        {
            if (wshRulesCache is null)
            {
                var keyStatic = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\RestrictedServices\Static\System");
                var keyConfig = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\RestrictedServices\Configurable\System");

                IEnumerable<string> allkeyvalues = Enumerable.Empty<string>();
                if (keyStatic != null)
                {
                    allkeyvalues = allkeyvalues.Concat(keyStatic.GetValueNames().Select(s => (string)keyStatic.GetValue(s)));
                }
                if (keyConfig != null)
                {
                    allkeyvalues = allkeyvalues.Concat(keyConfig.GetValueNames().Select(s => (string)keyConfig.GetValue(s)));
                }

                wshRulesCache = allkeyvalues.Select(s => new WSHRule(s)).ToArray();

                if (keyStatic != null)
                {
                    keyStatic.Close();
                }
                if (keyConfig != null)
                {
                    keyConfig.Close();
                }
            }

            var ret = firewallPolicy.Rules.Cast<INetFwRule>()
                                          .Select(r => new FwRule(r))
                                          .Concat(wshRulesCache);

            if (!AlsoGetInactive)
            {
                return ret.Where(r => r.Enabled).ToArray();
            }
            else
            {
                return ret.ToArray();
            }
        }

        public static bool IsCurrentProfilePublic() => (firewallPolicy.CurrentProfileTypes & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC) != 0;
        public static bool IsCurrentProfilePrivate() => (firewallPolicy.CurrentProfileTypes & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE) != 0;
        public static bool IsCurrentProfileDomain() => (firewallPolicy.CurrentProfileTypes & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN) != 0;

        public static int GetCurrentProfile() => firewallPolicy.CurrentProfileTypes;

        public static IEnumerable<Rule> GetMatchingRules(string path, string appPkgId, int protocol, string target, string targetPort, string localPort, string service, string localUserOwner, bool blockOnly, bool outgoingOnly = true)
        {
            var currentProfile = GetCurrentProfile(); //This call is relatively slow, and calling it many times causes a startup delay. Let's cache it!
            IEnumerable<Rule> ret = GetRules().Where(r => r.Matches(path, service, protocol, localPort, target, targetPort, appPkgId, localUserOwner, currentProfile));
            if (blockOnly)
            {
                ret = ret.Where(r => r.Action == NET_FW_ACTION_.NET_FW_ACTION_BLOCK);
            }
            if (outgoingOnly)
            {
                ret = ret.Where(r => r.Direction == NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT);
            }

            return ret;
        }

        /// <summary>
        /// Get the rules matching an eventlog item taking process appPkgId and svcName into account.
        /// </summary>
        /// <param name="path">Executable path</param>
        /// <param name="target">Target IP or *</param>
        /// <param name="targetPort">Target port or *</param>
        /// <param name="blockOnly">Filter for block only rules</param>
        /// <param name="outgoingOnly">Filter for outgoing rules only</param>
        /// <returns></returns>
        public static IEnumerable<Rule> GetMatchingRulesForEvent(uint pid, string path, string target, string targetPort, bool blockOnly = true, bool outgoingOnly = false)
        {
            var appPkgId = pid > 0 ? ProcessHelper.GetAppPkgId(pid) : string.Empty;
            var currentProfile = GetCurrentProfile();
            var svcName = "*";
            path = path ?? "";
            if (pid > 0 && path.EndsWith("svchost.exe", StringComparison.OrdinalIgnoreCase))
            {
                // get the scvName associated with svchost.exe
                var cLine = ProcessHelper.GetCommandLineFromProcessWMI(pid);
                if (cLine != null)
                {
                    svcName = cLine.Split(new string[] { " -s " }, StringSplitOptions.None).Last().Split(' ').First();
                }
            }

            var exeName = System.IO.Path.GetFileName(path);
            LogHelper.Debug($"\nGetMatchingRulesForEvent: path={exeName}, svcName={svcName}, pid={pid}, target={target} targetPort={targetPort}, blockOnly={blockOnly}, outgoingOnly={outgoingOnly}");

            //IEnumerable<Rule> ret = GetRules(AlsoGetInactive: false).Distinct(new SimpleEventRuleCompare()).Where(r => r.Enabled && RuleMatchesEvent(r, currentProfile, appPkgId, svcName, path, target, targetPort));
            IEnumerable<Rule> ret = GetRules(AlsoGetInactive: false).Where(r => r.MatchesEvent(currentProfile, appPkgId, svcName, path, target, targetPort));

            if (blockOnly)
            {
                ret = ret.Where(r => r.Action == NET_FW_ACTION_.NET_FW_ACTION_BLOCK);
            }
            if (outgoingOnly)
            {
                ret = ret.Where(r => r.Direction == NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT);
            }

            return ret;
        }

        public static string GetCurrentProfileAsText()
        {
            return Rule.GetProfileAsText(GetCurrentProfile());
        }

        public static void UpdatePrivatePolicy(bool enable, bool blockInbound, bool blockOutbound, bool disableInNotif)
        {
            UpdatePolicy(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE, enable, blockInbound, blockOutbound, disableInNotif);
        }

        public static void UpdatePublicPolicy(bool enable, bool blockInbound, bool blockOutbound, bool disableInNotif)
        {
            UpdatePolicy(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC, enable, blockInbound, blockOutbound, disableInNotif);
        }

        public static void UpdateDomainPolicy(bool enable, bool blockInbound, bool blockOutbound, bool disableInNotif)
        {
            UpdatePolicy(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN, enable, blockInbound, blockOutbound, disableInNotif);
        }


        private static void UpdatePolicy(NET_FW_PROFILE_TYPE2_ profile, bool enable, bool blockInbound, bool blockOutbound, bool disableInNotif)
        {
            firewallPolicy.FirewallEnabled[profile] = enable;
            firewallPolicy.DefaultInboundAction[profile] = blockInbound ? NET_FW_ACTION_.NET_FW_ACTION_BLOCK : NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
            firewallPolicy.DefaultOutboundAction[profile] = blockOutbound ? NET_FW_ACTION_.NET_FW_ACTION_BLOCK : NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
            firewallPolicy.NotificationsDisabled[profile] = disableInNotif;
        }

        public enum Status
        {
            DISABLED,
            ENABLED_ALLOW,
            ENABLED_BLOCK,
            ENABLED_NOTIFY
        }

        public static void UpdatePrivateStatus(out Status inStatus, out Status outStatus)
        {
            UpdateStatus(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE, out inStatus, out outStatus);
        }

        public static void UpdatePublicStatus(out Status inStatus, out Status outStatus)
        {
            UpdateStatus(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC, out inStatus, out outStatus);
        }

        public static void UpdateDomainStatus(out Status inStatus, out Status outStatus)
        {
            UpdateStatus(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN, out inStatus, out outStatus);
        }

        private static void UpdateStatus(NET_FW_PROFILE_TYPE2_ profile, out Status inStatus, out Status outStatus)
        {
            if (firewallPolicy.FirewallEnabled[profile])
            {
                if (firewallPolicy.DefaultInboundAction[profile] == NET_FW_ACTION_.NET_FW_ACTION_BLOCK)
                {
                    if (firewallPolicy.NotificationsDisabled[profile])
                    {
                        inStatus = Status.ENABLED_BLOCK;
                    }
                    else
                    {
                        inStatus = Status.ENABLED_NOTIFY;
                    }
                }
                else
                {
                    inStatus = Status.ENABLED_ALLOW;
                }

                outStatus = firewallPolicy.DefaultOutboundAction[profile] == NET_FW_ACTION_.NET_FW_ACTION_BLOCK ? Status.ENABLED_BLOCK : Status.ENABLED_ALLOW;
            }
            else
            {
                inStatus = Status.DISABLED;
                outStatus = Status.DISABLED;
            }
        }


    }
}