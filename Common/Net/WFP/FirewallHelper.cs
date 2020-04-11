using System;
using NetFwTypeLib;
using System.Linq;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Diagnostics;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Common.Net.WFP.Rules;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.WFP
{
    public static partial class FirewallHelper
    {
        //[DllImport("user32.dll", SetLastError = true)]
        //private static extern int LoadString(IntPtr hInstance, uint uID, StringBuilder lpBuffer, int nBufferMax);

        //[DllImport("kernel32.dll", SetLastError = true)]
        //private static extern IntPtr FindResource(IntPtr hModule, int lpName, int lpType);

        //[DllImport("kernel32.dll", SetLastError = true)]
        //private static extern IntPtr LoadLibraryEx(string lpFileName, [In] IntPtr hFile, uint dwFlags);

        //[DllImport("kernel32.dll", SetLastError = true)]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //private static extern bool FreeLibrary([In] IntPtr hModule);

        //private const uint LOAD_LIBRARY_AS_DATAFILE = 0&00000002;

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

        public static bool IsEventAccepted(EventLogEntry entry)
        {
            var instanceId = entry.InstanceId;

            // https://docs.microsoft.com/en-us/windows/security/threat-protection/auditing/audit-filtering-platform-connection
            return
                instanceId == 5157 // block connection
                || instanceId == 5152 // drop packet
                                      // Cannot parse this event: || instanceId == 5031 
                || instanceId == 5150
                || instanceId == 5151
                || instanceId == 5154
                || instanceId == 5155
                || instanceId == 5156;
        }

        public static string GetEventInstanceIdAsString(long eventId)
        {
            // https://docs.microsoft.com/en-us/windows/security/threat-protection/auditing/audit-filtering-platform-connection
            var reason = "Block: {0} ";
            switch (eventId)
            {
                case 5157:
                    return string.Format(reason, "connection");
                case 5152:
                    return string.Format(reason, "packet droped");
                case 5031:
                    return string.Format(reason, "app connection"); //  Firewall blocked an application from accepting incoming connections on the network.
                case 5150:
                    return string.Format(reason, "packet");
                case 5151:
                    return string.Format(reason, "packet (other FW)");
                case 5154:
                    return "Allow: listen";
                case 5155:
                    return string.Format(reason, "listen");
                case 5156:
                    return "Allow: connection";
                default:
                    return "[UNKNOWN] eventId:" + eventId.ToString();
            }
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

        public static int GetCurrentProfile()
        {
            return firewallPolicy.CurrentProfileTypes;
        }

        public static IEnumerable<Rule> GetMatchingRules(string path, string appPkgId, int protocol, string target, string targetPort, string localPort, IEnumerable<string> svc, string localUserOwner, bool blockOnly, bool outgoingOnly = true)
        {
            var currentProfile = GetCurrentProfile(); //This call is relatively slow, and calling it many times causes a startup delay. Let's cache it!
            IEnumerable<Rule> ret = GetRules().Where(r => r.Matches(path, svc, protocol, localPort, target, targetPort, appPkgId, localUserOwner, currentProfile));
            if (blockOnly)
            {
                ret = ret.Where(r => r.Action == NET_FW_ACTION_.NET_FW_ACTION_BLOCK);
            }
            if (outgoingOnly)
            {
                ret = ret.Where(r => r.Direction == NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT);
            }

            //Note: This fills up the logfile quite quickly...
            /*LogHelper.Debug("GetMatchingRules: Matching the following rule:");
            LogHelper.Debug("ToMatch: " + path + ", " + protocol + ", " + target + ", " + targetPort + ", " + localPort + ", " + String.Join(",", svc) + ", " + appPkgId + ", " + blockOnly.ToString(), ", " + outgoingOnly.ToString());
            foreach (var r in ret)
            {
                LogHelper.Debug("Matched rule: " + r.ApplicationName + ", " + r.Protocol + ", " + r.RemoteAddresses + ", " + r.RemotePorts + ", " + r.LocalPorts + ", " + r.ServiceName + ", " + r.AppPkgId + ", " + r.LUOwn + ", " + r.ActionStr + ", " + r.Description + ", " + r.Enabled);
            }*/

            return ret;
        }

        
        /*class SimpleEventRuleCompare : IEqualityComparer<Rule>
        {
            public bool Equals(Rule x, Rule y)
            {
                // Two items are equal if their keys are equal.
                return x.Name == y.Name;
            }

            public int GetHashCode(Rule obj)
            {
                return obj.Name.GetHashCode();
            }
        }*/

        /// <summary>
        /// Get the rules matching an eventlog item taking process appPkgId and svcName into account.
        /// </summary>
        /// <param name="path">Executable path</param>
        /// <param name="target">Target IP or *</param>
        /// <param name="targetPort">Target port or *</param>
        /// <param name="blockOnly">Filter for block only rules</param>
        /// <param name="outgoingOnly">Filter for outgoing rules only</param>
        /// <returns></returns>
        public static IEnumerable<Rule> GetMatchingRulesForEvent(int pid, string path, string target, string targetPort, bool blockOnly = true, bool outgoingOnly = false)
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


    }
}