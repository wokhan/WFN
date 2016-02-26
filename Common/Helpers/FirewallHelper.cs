using System;
using NetFwTypeLib;
using System.Linq;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.ServiceProcess;
using System.Windows.Media;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public class FirewallHelper
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

        public class Rule
        {
            private class WSHRule : INetFwRule
            {
                public WSHRule(string regRule)
                {
                    ILookup<string, string> parsed = regRule.TrimEnd('|').Split('|').Skip(1).Select(s => s.Split('=')).ToLookup(s => s[0].ToLower(), s => s[1]);

                    this.Action = parsed["action"].FirstOrDefault() == "Block" ? NET_FW_ACTION_.NET_FW_ACTION_BLOCK : NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                    this.ApplicationName = parsed["app"].FirstOrDefault();
                    this.Description = parsed["desc"].FirstOrDefault();
                    this.Direction = parsed["dir"].FirstOrDefault() == "In" ? NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN : NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                    this.Enabled = parsed.Contains("active") ? bool.Parse(parsed["active"].FirstOrDefault()) : true;

                    this.Name = "WSH - " + CommonHelper.GetMSResourceString(parsed["name"].FirstOrDefault());
                    this.LocalPorts = parsed["lport"].FirstOrDefault();
                    this.LocalAddresses = String.Join(", ", parsed["la4"].Concat(parsed["la6"]).ToArray());
                    this.RemotePorts = parsed["rport"].FirstOrDefault();
                    this.RemoteAddresses = String.Join(", ", parsed["ra4"].Concat(parsed["ra6"]).ToArray());

                    if (parsed.Contains("protocol") && parsed["protocol"].Any())
                    {
                        this.Protocol = int.Parse(parsed["protocol"].First());
                    }
                    else
                    {
                        this.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY;
                    }

                    this.serviceName = parsed["svc"].FirstOrDefault();

                    //// Win 8 ?
                    //if (regRule.StartsWith("v2.20"))
                    //{
                    //    this.AppPkgId = parsed["apppkdid"].FirstOrDefault();
                    //    this.Security = parsed["security"].FirstOrDefault();
                    //    this.LUAuth = parsed["luauth"].FirstOrDefault();
                    //    this.LUOwn = parsed["luown"].FirstOrDefault();
                    //}
                }

                //private string resolveString(string p)
                //{
                //    if (p != null && p.StartsWith("@"))
                //    {
                //        string[] res = p.Substring(1).Split(',');

                //        IntPtr lib = LoadLibraryEx(res[0], IntPtr.Zero, 0&00000002);
                //        //IntPtr strh = FindResource(lib, int.Parse(res[1]), 6);
                //        //if (strh != IntPtr.Zero)
                //        {
                //            StringBuilder sb = new StringBuilder(255);
                //            LoadString(lib, (UInt16)int.Parse(res[1]), sb, sb.Capacity);

                //            FreeLibrary(lib);

                //            return (sb.Length > 0 ? sb.ToString() : p);
                //        }
                //        //else
                //        {
                //            FreeLibrary(lib);

                //            return p;
                //        }
                //    }
                //    else
                //    {
                //        return p;
                //    }
                //}

                public NET_FW_ACTION_ Action { get; set; }
                public string ApplicationName { get; set; }
                public string Description { get; set; }
                public NET_FW_RULE_DIRECTION_ Direction { get; set; }
                public bool EdgeTraversal { get; set; }
                //public int EdgeTraversalOptions { get; set; }
                public bool Enabled { get; set; }
                public string Grouping { get; set; }
                public string IcmpTypesAndCodes { get; set; }
                public object Interfaces { get; set; }
                public string InterfaceTypes { get; set; }
                public string LocalAddresses { get; set; }
                public string LocalPorts { get; set; }
                public string Name { get; set; }
                public int Profiles { get; set; }
                public int Protocol { get; set; }
                public string RemoteAddresses { get; set; }
                public string RemotePorts { get; set; }
                public string serviceName { get; set; }

                //// For metro apps only (v2.20)
                //public string AppPkgId { get; set; }
                //public string Security { get; set; }
                //public string LUOwn { get; set; }
                //public string LUAuth { get; set; }
                //public string EmbedCtxt { get; set; }
            }

            private NetFwTypeLib.INetFwRule InnerRule;

            public Rule(INetFwRule innerRule)
            {
                InnerRule = innerRule;
            }

            public Rule(string regRule)
            {
                InnerRule = new WSHRule(regRule);
            }

            public NET_FW_ACTION_ Action { get { return InnerRule.Action; } }

            private string _applicationName = null;
            public string ApplicationName
            {
                get
                {
                    if (_applicationName == null)
                    {
                        _applicationName = (InnerRule.ApplicationName != null ? Environment.ExpandEnvironmentVariables(InnerRule.ApplicationName) : String.Empty);

                    }
                    return _applicationName;
                }
            }

            private string _description = null;
            public string Description
            {
                get
                {
                    if (_description == null)
                    {
                        _description = CommonHelper.GetMSResourceString(InnerRule.Description);
                    }

                    return _description;
                }
            }

            public NET_FW_RULE_DIRECTION_ Direction { get { return InnerRule.Direction; } }
            public bool EdgeTraversal { get { return InnerRule.EdgeTraversal; } }
            //public int EdgeTraversalOptions { get { return InnerRule.EdgeTraversalOptions; } }
            public bool Enabled { get { return InnerRule.Enabled; } }
            public string Grouping { get { return InnerRule.Grouping; } }
            public string IcmpTypesAndCodes { get { return InnerRule.IcmpTypesAndCodes; } }
            public object Interfaces { get { return InnerRule.Interfaces; } }
            public string InterfaceTypes { get { return InnerRule.InterfaceTypes; } }
            public string LocalAddresses { get { return InnerRule.LocalAddresses; } }
            public string LocalPorts { get { return InnerRule.LocalPorts; } }

            private string _name = null;
            public string Name
            {
                get
                {
                    if (_name == null)
                    {
                        _name = CommonHelper.GetMSResourceString(InnerRule.Name);
                    }

                    return _name;
                }
            }

            public int Profiles { get { return InnerRule.Profiles; } }
            public int Protocol { get { return InnerRule.Protocol; } }
            public string RemoteAddresses { get { return InnerRule.RemoteAddresses; } }
            public string RemotePorts { get { return InnerRule.RemotePorts; } }
            public string serviceName { get { return InnerRule.serviceName; } }

            private ImageSource _icon = null;
            public ImageSource Icon
            {
                get
                {
                    if (_icon == null)
                    {
                        _icon = ProcessHelper.GetIcon(InnerRule.ApplicationName);
                    }

                    return _icon;
                }
            }

            public string ProfilesStr { get { return getProfile(InnerRule.Profiles); } }

            public string ActionStr { get { return getAction(InnerRule.Action); } }

            public string DirectionStr { get { return getDirection(InnerRule.Direction); } }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="nET_FW_RULE_DIRECTION_"></param>
            /// <returns></returns>
            private static string getDirection(NET_FW_RULE_DIRECTION_ nET_FW_RULE_DIRECTION_)
            {
                switch (nET_FW_RULE_DIRECTION_)
                {
                    case NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN:
                        return Resources.FW_DIR_IN;

                    case NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT:
                        return Resources.FW_DIR_OUT;

                    case NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_MAX:
                        return Resources.FW_DIR_BOTH;

                    default:
                        return "?";
                }
            }


            /// <summary>
            /// 
            /// </summary>
            /// <param name="nET_FW_ACTION_"></param>
            /// <returns></returns>
            private static string getAction(NET_FW_ACTION_ nET_FW_ACTION_)
            {
                switch (nET_FW_ACTION_)
                {
                    case NET_FW_ACTION_.NET_FW_ACTION_ALLOW:
                        return Resources.FW_RULE_ALLOW;

                    case NET_FW_ACTION_.NET_FW_ACTION_BLOCK:
                        return Resources.FW_RULE_BLOCK;

                    default:
                        return "?";
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="nET_FW_PROFILE_TYPE2_"></param>
            /// <returns></returns>
            internal static string getProfile(int nET_FW_PROFILE_TYPE2_)
            {
                return FirewallHelper.GetProfileAsText(nET_FW_PROFILE_TYPE2_);
            }
        }

        private static INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
        private const string indParamFormat = "{0}#$#{1}#$#{2}#$#{3}#$#{4}#$#{5}#$#{6}#$#{7}#$#{8}";
        private static string WFNRuleManagerEXE = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RuleManager.exe");

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

                return true;
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to remove the rule.", e);
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ruleName"></param>
        /// <param name="currentPath"></param>
        /// <param name="service"></param>
        /// <param name="protocol"></param>
        /// <param name="target"></param>
        /// <param name="targetPort"></param>
        /// <param name="localport"></param>
        /// <returns></returns>
        public static bool AddBlockRuleIndirect(string ruleName, string currentPath, string[] services, int protocol, string target, string targetPort, string localport, bool useCurrentProfile)
        {
            string param = Convert.ToBase64String(Encoding.Unicode.GetBytes(String.Format(indParamFormat, ruleName, currentPath, services != null ? String.Join(",", services) : null, protocol, target, targetPort, localport, useCurrentProfile, "B")));
            return ProcessHelper.getProcessFeedback(WFNRuleManagerEXE, param, true, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ruleName"></param>
        /// <param name="currentPath"></param>
        /// <param name="service"></param>
        /// <param name="protocol"></param>
        /// <param name="target"></param>
        /// <param name="targetPort"></param>
        /// <param name="localport"></param>
        /// <returns></returns>
        public static bool AddBlockRule(string ruleName, string currentPath, string service, int protocol, string target, string targetPort, string localport, bool useCurrentProfile)
        {
            return AddRule(ruleName, currentPath, service, protocol, target, targetPort, localport, NET_FW_ACTION_.NET_FW_ACTION_BLOCK, false, useCurrentProfile);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ruleName"></param>
        /// <param name="currentPath"></param>
        /// <param name="service"></param>
        /// <param name="protocol"></param>
        /// <param name="target"></param>
        /// <param name="targetPort"></param>
        /// <param name="localport"></param>
        /// <returns></returns>
        public static bool AddAllowRuleIndirect(string ruleName, string currentPath, string[] services, int protocol, string target, string targetPort, string localport, bool useCurrentProfile)
        {
            string param = Convert.ToBase64String(Encoding.Unicode.GetBytes(String.Format(indParamFormat, ruleName, currentPath, services != null ? String.Join(",", services) : null, protocol, target, targetPort, localport, useCurrentProfile, "A")));
            return ProcessHelper.getProcessFeedback(WFNRuleManagerEXE, param, true, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ruleName"></param>
        /// <param name="currentPath"></param>
        /// <param name="service"></param>
        /// <param name="protocol"></param>
        /// <param name="target"></param>
        /// <param name="targetPort"></param>
        /// <param name="localport"></param>
        /// <returns></returns>
        public static bool AddAllowRule(string ruleName, string currentPath, string service, int protocol, string target, string targetPort, string localport, bool useCurrentProfile)
        {
            return AddRule(ruleName, currentPath, service, protocol, target, targetPort, localport, NET_FW_ACTION_.NET_FW_ACTION_ALLOW, false, useCurrentProfile);
        }


        public static bool AddTempRule(string ruleName, string currentPath, string service, int protocol, string target, string targetPort, string localport, bool useCurrentProfile)
        {
            return AddRule(ruleName, currentPath, service, protocol, target, targetPort, localport, NET_FW_ACTION_.NET_FW_ACTION_ALLOW, true, useCurrentProfile);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ruleName"></param>
        /// <param name="currentPath"></param>
        /// <param name="service"></param>
        /// <param name="protocol"></param>
        /// <param name="target"></param>
        /// <param name="targetPort"></param>
        /// <param name="localport"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static bool AddRule(string ruleName, string currentPath, string service, int protocol, string target, string targetPort, string localport, NET_FW_ACTION_ action, bool isTemp, bool currentProfile = true)
        {
            try
            {
                INetFwRule firewallRule = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                firewallRule.Action = action;
                firewallRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                firewallRule.Enabled = true;
                firewallRule.Profiles = currentProfile ? FirewallHelper.GetCurrentProfile() : FirewallHelper.GetGlobalProfile();
                firewallRule.InterfaceTypes = "All";
                firewallRule.Name = ruleName;
                firewallRule.ApplicationName = currentPath;

                if (!String.IsNullOrEmpty(service))
                {
                    firewallRule.serviceName = service;
                }

                if (protocol != -1)
                {
                    firewallRule.Protocol = getProtocol(protocol);
                }

                if (!String.IsNullOrEmpty(localport))
                {
                    firewallRule.LocalPorts = localport;

                    if (!isTemp)
                    {
                        firewallRule.Name += " [L:" + targetPort + "]";
                    }
                }

                if (!String.IsNullOrEmpty(target))
                {
                    firewallRule.RemoteAddresses = target;

                    if (!isTemp)
                    {
                        firewallRule.Name += " [T:" + target + "]";
                    }
                }

                if (!String.IsNullOrEmpty(targetPort))
                {
                    firewallRule.RemotePorts = targetPort;

                    if (!isTemp)
                    {
                        firewallRule.Name += " [R:" + targetPort + "]";
                    }
                }

                firewallPolicy.Rules.Add(firewallRule);

                return true;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to create the rule", e);
            }

            return false;
        }

        private static int GetGlobalProfile()
        {
            return (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL;
        }

        public static string getType(int type)
        {
            return Enum.GetName(typeof(NET_FW_PROFILE_TYPE2_), type);
        }

        public static string getProtocolAsString(string protocol)
        {
            return getProtocolAsString(int.Parse(protocol));
        }

        public static string getProtocolAsString(int protocol)
        {
            switch (protocol)
            {
                case (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP:
                    return "TCP";

                case (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP:
                    return "UDP";

                default:
                    return null;
            }
        }

        public static int getProtocol(int protocol)
        {
            try
            {
                return (int)(NET_FW_IP_PROTOCOL_)protocol;
            }
            catch
            {
                return (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY;
            }
        }

        public static bool AddTempRuleIndirect(string ruleName, string currentPath, string[] services, int protocol, string target, string targetPort, string localport, bool useCurrentProfile)
        {
            string param = Convert.ToBase64String(Encoding.Unicode.GetBytes(String.Format(indParamFormat, ruleName, currentPath, services != null ? String.Join(",", services) : null, protocol, target, targetPort, localport, useCurrentProfile, "T")));
            return ProcessHelper.getProcessFeedback(WFNRuleManagerEXE, param, true, true);
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

            }

            return false;
        }

        private static Rule[] wshRulesCache = null;

        public enum Protocols
        {
            TCP = NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP,
            UDP = NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP,
            ANY = NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY
        }

        public static Rule[] GetRules()
        {
            if (wshRulesCache == null)
            {
                var keyStatic = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\RestrictedServices\Static\System");
                var keyConfig = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\RestrictedServices\Configurable\System");

                var allkeyvalues = keyStatic.GetValueNames()
                                            .Select(s => (string)keyStatic.GetValue(s))
                                            .Concat(keyConfig.GetValueNames()
                                                              .Select(s => (string)keyConfig.GetValue(s)));

                // Windows 8 or higher (ignoring Metro apps)
                if ((Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 2) || (Environment.OSVersion.Version.Major > 6))
                {
                    allkeyvalues = allkeyvalues.Where(r => !r.Contains("AppPkgId"));
                }

                wshRulesCache = allkeyvalues.Select(s => new Rule(s)).ToArray();

                keyStatic.Close();
                keyConfig.Close();
            }

            return firewallPolicy.Rules.Cast<INetFwRule>()
                                       .Select(r => new Rule(r))
                                       .Concat(wshRulesCache)
                                       .ToArray();
        }

        public static int GetCurrentProfile()
        {
            return firewallPolicy.CurrentProfileTypes;
        }

        public static bool IsIPProtocol(int protocol)
        {
            return (protocol == (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP || protocol == (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP);
        }

        public static IEnumerable<Rule> GetMatchingRules(string path, string protocol, string target, string targetPort, string localport, IEnumerable<string> svc, bool blockOnly)
        {
            var ret = GetRules().Where(r => RuleMatches(r, path, svc, protocol, localport, target, targetPort));
            if (blockOnly)
            {
                ret = ret.Where(r => r.Action == NET_FW_ACTION_.NET_FW_ACTION_BLOCK);
            }

            return ret;
        }


        public static bool RuleMatches(Rule r, string path, IEnumerable<string> svc, string protocol, string localport, string target, string remoteport)
        {
            bool ret = r.Enabled
                       && (((r.Profiles & GetCurrentProfile()) != 0) || ((r.Profiles & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL) != 0))
                       && (String.IsNullOrEmpty(r.ApplicationName) || StringComparer.CurrentCultureIgnoreCase.Compare(r.ApplicationName, path) == 0)
                       && ((String.IsNullOrEmpty(r.serviceName) && (svc == null || !svc.Any())) || svc.Contains(r.serviceName, StringComparer.CurrentCultureIgnoreCase))
                       && (r.Protocol == (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY || r.Protocol.ToString() == protocol)
                       && CheckRuleMultivalue(r.RemoteAddresses, target)
                       && CheckRuleMultivalue(r.RemotePorts, remoteport)
                       && CheckRuleMultivalue(r.LocalPorts, localport);

            return ret;
        }


        private static bool CheckRuleMultivalue(string rulePorts, string checkedport)
        {
            return String.IsNullOrEmpty(rulePorts) || rulePorts == "*" || rulePorts.Split(',').Contains(checkedport);
        }

        public static string GetCurrentProfileAsText()
        {
            return GetProfileAsText(GetCurrentProfile());
        }


        public static string GetProfileAsText(int nET_FW_PROFILE_TYPE2_)
        {

            string[] ret = new string[3];
            int i = 0;
            if (nET_FW_PROFILE_TYPE2_ == (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL)
            {
                ret[i] = Resources.FW_PROFILE_ALL;
            }
            else
            {
                if ((nET_FW_PROFILE_TYPE2_ & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN) != 0)
                {
                    ret[i++] = Resources.FW_PROFILE_DOMAIN;
                }
                if ((nET_FW_PROFILE_TYPE2_ & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE) != 0)
                {
                    ret[i++] = Resources.FW_PROFILE_PRIVATE;
                }
                if ((nET_FW_PROFILE_TYPE2_ & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC) != 0)
                {
                    ret[i++] = Resources.FW_PROFILE_PUBLIC;
                }
            }
            return String.Join(", ", ret, 0, i);
        }

        public class FirewallStatusWrapper
        {
            private static Dictionary<bool, string> _actions = new Dictionary<bool, string>{
                { true, "Block" },
                { false, "Allow"}
            };

            public static Dictionary<bool, string> Actions { get { return _actions; } }

            public enum Status
            {
                DISABLED,
                ENABLED_ALLOW,
                ENABLED_BLOCK,
                ENABLED_NOTIFY
            }

            private Status privateInStatus = Status.DISABLED;
            private Status domainInStatus = Status.DISABLED;
            private Status publicInStatus = Status.DISABLED;

            private Status privateOutStatus = Status.DISABLED;
            private Status domainOutStatus = Status.DISABLED;
            private Status publicOutStatus = Status.DISABLED;

            public bool PrivateIsEnabled { get; set; }
            public bool PrivateIsInBlocked { get; set; }
            public bool PrivateIsOutBlocked { get; set; }
            public bool PrivateIsInBlockedNotif { get; set; }
            public bool PrivateIsOutBlockedNotif { get; set; }

            public bool PublicIsEnabled { get; set; }
            public bool PublicIsInBlocked { get; set; }
            public bool PublicIsOutBlocked { get; set; }
            public bool PublicIsInBlockedNotif { get; set; }
            public bool PublicIsOutBlockedNotif { get; set; }

            public bool DomainIsEnabled { get; set; }
            public bool DomainIsInBlocked { get; set; }
            public bool DomainIsOutBlocked { get; set; }
            public bool DomainIsInBlockedNotif { get; set; }
            public bool DomainIsOutBlockedNotif { get; set; }

            public string CurrentProfile { get { return GetCurrentProfileAsText(); } }

            public FirewallStatusWrapper()
            {
                updateStatus(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE, ref privateInStatus, ref privateOutStatus);
                updateStatus(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC, ref publicInStatus, ref publicOutStatus);
                updateStatus(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN, ref domainInStatus, ref domainOutStatus);

                PrivateIsEnabled = (privateInStatus != Status.DISABLED);
                PrivateIsInBlocked = (privateInStatus == Status.ENABLED_BLOCK);
                PrivateIsInBlockedNotif = (privateInStatus == Status.ENABLED_NOTIFY);
                PrivateIsOutBlocked = (privateOutStatus == Status.ENABLED_BLOCK);

                PublicIsEnabled = (publicInStatus != Status.DISABLED);
                PublicIsInBlocked = (publicInStatus == Status.ENABLED_BLOCK);
                PublicIsInBlockedNotif = (publicInStatus == Status.ENABLED_NOTIFY);
                PublicIsOutBlocked = (publicOutStatus == Status.ENABLED_BLOCK);

                DomainIsEnabled = (domainInStatus != Status.DISABLED);
                DomainIsInBlocked = (domainInStatus == Status.ENABLED_BLOCK);
                DomainIsInBlockedNotif = (domainInStatus == Status.ENABLED_NOTIFY);
                DomainIsOutBlocked = (domainOutStatus == Status.ENABLED_BLOCK);
            }

            private void updateStatus(NET_FW_PROFILE_TYPE2_ profile, ref Status stat, ref Status statOut)
            {
                if (firewallPolicy.FirewallEnabled[profile])
                {
                    if (firewallPolicy.DefaultInboundAction[profile] == NET_FW_ACTION_.NET_FW_ACTION_BLOCK)
                    {
                        if (firewallPolicy.NotificationsDisabled[profile])
                        {
                            stat = Status.ENABLED_BLOCK;
                        }
                        else
                        {
                            stat = Status.ENABLED_NOTIFY;
                        }
                    }
                    else
                    {
                        stat = Status.ENABLED_ALLOW;
                    }

                    if (firewallPolicy.DefaultOutboundAction[profile] == NET_FW_ACTION_.NET_FW_ACTION_BLOCK)
                    {
                        statOut = Status.ENABLED_BLOCK;
                    }
                    else
                    {
                        statOut = Status.ENABLED_ALLOW;
                    }
                }
            }

            public void Save()
            {
                firewallPolicy.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE] = PrivateIsEnabled;
                firewallPolicy.DefaultInboundAction[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE] = PrivateIsInBlockedNotif || PrivateIsInBlocked ? NET_FW_ACTION_.NET_FW_ACTION_BLOCK : NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                firewallPolicy.DefaultOutboundAction[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE] = PrivateIsOutBlockedNotif || PrivateIsOutBlocked ? NET_FW_ACTION_.NET_FW_ACTION_BLOCK : NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                firewallPolicy.NotificationsDisabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE] = !PrivateIsInBlockedNotif;

                firewallPolicy.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC] = PublicIsEnabled;
                firewallPolicy.DefaultInboundAction[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC] = PublicIsInBlockedNotif || PublicIsInBlocked ? NET_FW_ACTION_.NET_FW_ACTION_BLOCK : NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                firewallPolicy.DefaultOutboundAction[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC] = PublicIsOutBlockedNotif || PublicIsOutBlocked ? NET_FW_ACTION_.NET_FW_ACTION_BLOCK : NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                firewallPolicy.NotificationsDisabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC] = !PublicIsInBlockedNotif;

                firewallPolicy.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN] = DomainIsEnabled;
                firewallPolicy.DefaultInboundAction[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN] = DomainIsInBlockedNotif || DomainIsInBlocked ? NET_FW_ACTION_.NET_FW_ACTION_BLOCK : NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                firewallPolicy.DefaultOutboundAction[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN] = DomainIsOutBlockedNotif || DomainIsOutBlocked ? NET_FW_ACTION_.NET_FW_ACTION_BLOCK : NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                firewallPolicy.NotificationsDisabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN] = !DomainIsInBlockedNotif;
            }
        }
    }
}