using System;
using NetFwTypeLib;
using System.Linq;
using Microsoft.Win32;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Wokhan.WindowsFirewallNotifier.Common.Annotations;
using Wokhan.WindowsFirewallNotifier.Common.Properties;

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

        //private const uint LOAD_LIBRARY_AS_DATAFILE = 0&00000002;

        private static INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
        private const string indParamFormat = "{0}#$#{1}#$#{2}#$#{3}#$#{4}#$#{5}#$#{6}#$#{7}#$#{8}#$#{9}#$#{10}";
        private static string WFNRuleManagerEXE = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RuleManager.exe");

        public abstract class Rule
        {
            public abstract NET_FW_ACTION_ Action { get; }
            public abstract string ApplicationName { get; }
            public abstract string Description { get; }
            public abstract NET_FW_RULE_DIRECTION_ Direction { get; }
            public abstract bool EdgeTraversal { get; }
            public abstract bool Enabled { get; } //Active
            public abstract string Grouping { get; }
            public abstract string IcmpTypesAndCodes { get; }
            public abstract object Interfaces { get; }
            public abstract string InterfaceTypes { get; }
            public abstract string LocalAddresses { get; }
            public abstract string LocalPorts { get; }
            public abstract string Name { get; }
            public abstract int Profiles { get; }
            public abstract int Protocol { get; }
            public abstract string RemoteAddresses { get; }
            public abstract string RemotePorts { get; }
            public abstract string ServiceName { get; }

            //FIXME: v2.10?
            public abstract int EdgeTraversalOptions { get; }

            // For metro apps (v2.20)
            public abstract string AppPkgId { get; }
            //public abstract string Security { get; }
            public abstract string LUOwn { get; }
            //public abstract string LUAuth { get; }
            //public abstract string EmbedCtxt { get; }

            // For Windows 10 Creators Update (v2.27)
            //public abstract string Defer { get; }

            //FIXME: Need to parse: (RA42=) RmtIntrAnet

            private ImageSource _icon = null;
            public ImageSource Icon
            {
                get
                {
                    if (_icon == null)
                    {
                        _icon = IconHelper.GetIcon(this.ApplicationName); //FIXME: This is now expanded... Is that a problem?!?
                    }

                    return _icon;
                }
            }

            public string ProfilesStr { get { return getProfile(this.Profiles); } }

            public string ActionStr { get { return getAction(this.Action); } }

            public string DirectionStr { get { return getDirection(this.Direction); } }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="direction"></param>
            /// <returns></returns>
            private static string getDirection(NET_FW_RULE_DIRECTION_ direction)
            {
                switch (direction)
                {
                    case NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN:
                        return Resources.FW_DIR_IN;

                    case NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT:
                        return Resources.FW_DIR_OUT;

                    case NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_MAX:
                        return Resources.FW_DIR_BOTH;

                    default:
                        LogHelper.Warning("Unknown direction type: " + direction.ToString());
                        return "?";
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="action"></param>
            /// <returns></returns>
            private static string getAction(NET_FW_ACTION_ action)
            {
                switch (action)
                {
                    case NET_FW_ACTION_.NET_FW_ACTION_ALLOW:
                        return Resources.FW_RULE_ALLOW;

                    case NET_FW_ACTION_.NET_FW_ACTION_BLOCK:
                        return Resources.FW_RULE_BLOCK;

                    default:
                        LogHelper.Warning("Unknown action type: " + action.ToString());
                        return "?";
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="profile_type"></param>
            /// <returns></returns>
            internal static string getProfile(int profile_type)
            {
                return FirewallHelper.GetProfileAsText(profile_type);
            }

            public bool ApplyIndirect(bool isTemp)
            {
                string actionString;
                switch (Action)
                {
                    case NET_FW_ACTION_.NET_FW_ACTION_ALLOW:
                        actionString = "A";
                        break;

                    case NET_FW_ACTION_.NET_FW_ACTION_BLOCK:
                        actionString = "B";
                        break;

                    default:
                        throw new Exception("Unknown action type: " + Action.ToString());
                }
                if (isTemp)
                {
                    actionString = "T";
                }
                string param = Convert.ToBase64String(Encoding.Unicode.GetBytes(String.Format(indParamFormat, Name, ApplicationName, AppPkgId, LUOwn, ServiceName, Protocol, RemoteAddresses, RemotePorts, LocalPorts, Profiles, actionString)));
                return ProcessHelper.getProcessFeedback(WFNRuleManagerEXE, param, true, isTemp);
            }

            public abstract bool Apply(bool isTemp);
        }

        public class WSHRule : Rule
        {
            private ILookup<string, string> parsed;

            private Version version;

            public WSHRule(string regRule)
            {
                var parts = regRule.TrimEnd('|').Split('|');
                if (!(parts[0].StartsWith("v") || parts[0].StartsWith("V")))
                {
                    throw new Exception("Unknown rule versioning scheme: " + parts[0]);
                }
                this.version = new Version(parts[0].Substring(1));
                parsed = parts.Skip(1).Select(s => s.Split('=')).ToLookup(s => s[0].ToLower(), s => s[1]);
            }

            public override NET_FW_ACTION_ Action
            {
                get
                {
                    return parsed["action"].FirstOrDefault() == "Block" ? NET_FW_ACTION_.NET_FW_ACTION_BLOCK : NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                }
            }

            public override string ApplicationName
            {
                get
                {
                    return parsed["app"].FirstOrDefault();
                }
            }

            public override string AppPkgId
            {
                get
                {
                    return parsed["apppkgid"].FirstOrDefault();
                }
            }

            public override string Description
            {
                get
                {
                    return parsed["desc"].FirstOrDefault();
                }
            }

            public override NET_FW_RULE_DIRECTION_ Direction
            {
                get
                {
                    return parsed["dir"].FirstOrDefault() == "In" ? NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN : NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                }
            }

            public override bool EdgeTraversal
            {
                get
                {
                    return true; //FIXME: !
                }
            }

            public override int EdgeTraversalOptions
            {
                get
                {
                    return 0; //FIXME: !
                }
            }

            public override bool Enabled
            {
                get
                {
                    return parsed.Contains("active") ? bool.Parse(parsed["active"].FirstOrDefault()) : true;
                }
            }

            public override string Grouping
            {
                get
                {
                    return ""; //FIXME: !
                }
            }

            public override string IcmpTypesAndCodes
            {
                get
                {
                    return ""; //FIXME: !
                }
            }

            public override object Interfaces
            {
                get
                {
                    return null; //FIXME: !
                }
            }

            public override string InterfaceTypes
            {
                get
                {
                    return ""; //FIXME: !
                }
            }

            public override string LocalAddresses
            {
                get
                {
                    return String.Join(", ", parsed["la4"].Concat(parsed["la6"]).ToArray());
                }
            }

            public override string LocalPorts
            {
                get
                {
                    return parsed["lport"].FirstOrDefault();
                }
            }

            public override string LUOwn
            {
                get
                {
                    return parsed["luown"].FirstOrDefault();
                }
            }

            public override string Name
            {
                get
                {
                    return "WSH - " + CommonHelper.GetMSResourceString(parsed["name"].FirstOrDefault());
                }
            }

            public override int Profiles
            {
                get
                {
                    if (parsed["profile"].Count() == 0)
                    {
                        return (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL;
                    }
                    int profiles = 0;
                    foreach (var profile in parsed["profile"])
                    {
                        switch (profile)
                        {
                            case "Public":
                                profiles += (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC;
                                break;

                            case "Domain":
                                profiles += (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN;
                                break;

                            case "Private":
                                profiles += (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE;
                                break;

                            default:
                                LogHelper.Warning("Unknown profile type: " + profile);
                                break;
                        }
                    }
                    return profiles;
                }
            }

            public override int Protocol
            {
                get
                {
                    if (parsed.Contains("protocol") && parsed["protocol"].Any())
                    {
                        return int.Parse(parsed["protocol"].First());
                    }
                    else
                    {
                        return (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY;
                    }
                }
            }

            public override string RemoteAddresses
            {
                get
                {
                    return String.Join(", ", parsed["ra4"].Concat(parsed["ra6"]).ToArray());
                }
            }

            public override string RemotePorts
            {
                get
                {
                    return parsed["rport"].FirstOrDefault();
                }
            }

            public override string ServiceName
            {
                get
                {
                    return parsed["svc"].FirstOrDefault();
                }
            }

            //FIXME: v2.10?
            //public int EdgeTraversalOptions { get; set; }

            //// Win 8 ?
            //if (this.version >= new Version(2, 20))
            //{
            //    this.AppPkgId = parsed["apppkdid"].FirstOrDefault();
            //    this.Security = parsed["security"].FirstOrDefault();
            //    this.LUAuth = parsed["luauth"].FirstOrDefault();
            //    this.LUOwn = parsed["luown"].FirstOrDefault();
            //}

            //private string resolveString(string p)
            //{
            //    if (p != null && p.StartsWith("@"))
            //    {
            //        string[] res = p.Substring(1).Split(',');

            //        IntPtr lib = LoadLibraryEx(res[0], IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
            //        try
            //        {
            //            //IntPtr strh = FindResource(lib, int.Parse(res[1]), 6);
            //            //if (strh != IntPtr.Zero)
            //            {
            //                StringBuilder sb = new StringBuilder(255); //FIXME: Hardcoded string size!
            //                LoadString(lib, (UInt16)int.Parse(res[1]), sb, sb.Capacity);

            //                return (sb.Length > 0 ? sb.ToString() : p);
            //            }
            //            //else
            //            {
            //                return p;
            //            }
            //        }
            //        finally
            //        {
            //            FreeLibrary(lib);
            //        }
            //    }
            //    else
            //    {
            //        return p;
            //    }
            //}

            public override bool Apply(bool isTemp)
            {
                try
                {
                    INetFwRule firewallRule;

                    if (!String.IsNullOrEmpty(AppPkgId))
                    {
                        //Need INetFwRule3
                        firewallRule = (INetFwRule3)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                    }
                    else
                    {
                        firewallRule = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                    }
                    firewallRule.Action = Action;
                    firewallRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                    firewallRule.Enabled = true;
                    firewallRule.Profiles = Profiles;
                    firewallRule.InterfaceTypes = "All";
                    firewallRule.Name = Name;
                    firewallRule.ApplicationName = ApplicationName;

                    if (!String.IsNullOrEmpty(AppPkgId))
                    {
                        ((INetFwRule3)firewallRule).LocalAppPackageId = AppPkgId;

                        //This needs to be set as well
                        ((INetFwRule3)firewallRule).LocalUserOwner = LUOwn;
                    }

                    if (!String.IsNullOrEmpty(ServiceName))
                    {
                        firewallRule.serviceName = ServiceName;
                    }

                    if (Protocol != -1)
                    {
                        firewallRule.Protocol = normalizeProtocol(Protocol);
                    }

                    if (!String.IsNullOrEmpty(LocalPorts))
                    {
                        firewallRule.LocalPorts = LocalPorts;

                        if (!isTemp)
                        {
                            firewallRule.Name += " [L:" + LocalPorts + "]";
                        }
                    }

                    if (!String.IsNullOrEmpty(RemoteAddresses))
                    {
                        firewallRule.RemoteAddresses = RemoteAddresses;

                        if (!isTemp)
                        {
                            firewallRule.Name += " [T:" + RemoteAddresses + "]";
                        }
                    }

                    if (!String.IsNullOrEmpty(RemotePorts))
                    {
                        firewallRule.RemotePorts = RemotePorts;

                        if (!isTemp)
                        {
                            firewallRule.Name += " [R:" + RemotePorts + "]";
                        }
                    }

                    LogHelper.Debug("Adding rule to firewall...");
                    firewallPolicy.Rules.Add(firewallRule);

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
        }

        public class FwRule : Rule
        {
            private NetFwTypeLib.INetFwRule InnerRule;

            public FwRule(INetFwRule innerRule)
            {
                InnerRule = innerRule;
            }

            public override NET_FW_ACTION_ Action { get { return InnerRule.Action; } }

            private string _applicationName = null;
            public override string ApplicationName
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

            public override string AppPkgId
            {
                get
                {
                    if (InnerRule is INetFwRule3)
                    {
                        return ((INetFwRule3)InnerRule).LocalAppPackageId;
                    }
                    else
                    {
                        return String.Empty;
                    }
                }
            }

            private string _description = null;
            public override string Description
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

            public override NET_FW_RULE_DIRECTION_ Direction { get { return InnerRule.Direction; } }
            public override bool EdgeTraversal { get { return InnerRule.EdgeTraversal; } }
            public override int EdgeTraversalOptions
            {
                get
                {
                    if (InnerRule is INetFwRule2)
                    {
                        return ((INetFwRule2)InnerRule).EdgeTraversalOptions;
                    }
                    else
                    {
                        return 0; //FIXME: https://msdn.microsoft.com/en-us/library/windows/desktop/dd607258(v=vs.85).aspx   Proper default value...?
                    }
                }
            }
            public override bool Enabled { get { return InnerRule.Enabled; } }
            public override string Grouping { get { return InnerRule.Grouping; } }
            public override string IcmpTypesAndCodes { get { return InnerRule.IcmpTypesAndCodes; } }
            public override object Interfaces { get { return InnerRule.Interfaces; } }
            public override string InterfaceTypes { get { return InnerRule.InterfaceTypes; } }
            public override string LocalAddresses { get { return InnerRule.LocalAddresses; } }
            public override string LocalPorts { get { return InnerRule.LocalPorts; } }

            public override string LUOwn
            {
                get
                {
                    if (InnerRule is INetFwRule3)
                    {
                        return ((INetFwRule3)InnerRule).LocalUserOwner;
                    }
                    else
                    {
                        return String.Empty;
                    }
                }
            }

            private string _name = null;
            public override string Name
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

            public override int Profiles { get { return InnerRule.Profiles; } }
            public override int Protocol { get { return InnerRule.Protocol; } }
            public override string RemoteAddresses { get { return InnerRule.RemoteAddresses; } }
            public override string RemotePorts { get { return InnerRule.RemotePorts; } }
            public override string ServiceName { get { return InnerRule.serviceName; } }

            public override bool Apply(bool isTemp)
            {
                try
                {
                    LogHelper.Debug("Adding rule to firewall...");
                    firewallPolicy.Rules.Add(InnerRule);

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
        }

        public class CustomRule : Rule
        {
            public override NET_FW_ACTION_ Action { get; }
            public override string ApplicationName { get; }
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
            public override string ServiceName { get; }

            public override bool Apply(bool isTemp)
            {
                try
                {
                    INetFwRule firewallRule;

                    if (!String.IsNullOrEmpty(AppPkgId))
                    {
                        //Need INetFwRule3
                        firewallRule = (INetFwRule3)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                    }
                    else
                    {
                        firewallRule = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                    }
                    firewallRule.Action = Action;
                    firewallRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                    firewallRule.Enabled = true;
                    firewallRule.Profiles = Profiles;
                    firewallRule.InterfaceTypes = "All";
                    firewallRule.Name = Name;
                    firewallRule.ApplicationName = ApplicationName;

                    if (!String.IsNullOrEmpty(AppPkgId))
                    {
                        ((INetFwRule3)firewallRule).LocalAppPackageId = AppPkgId;

                        //This needs to be set as well
                        ((INetFwRule3)firewallRule).LocalUserOwner = LUOwn;
                    }

                    if (!String.IsNullOrEmpty(ServiceName))
                    {
                        firewallRule.serviceName = ServiceName;
                    }

                    if (Protocol != -1)
                    {
                        firewallRule.Protocol = normalizeProtocol(Protocol);
                    }

                    if (!String.IsNullOrEmpty(LocalPorts))
                    {
                        firewallRule.LocalPorts = LocalPorts;

                        if (!isTemp)
                        {
                            firewallRule.Name += " [L:" + LocalPorts + "]";
                        }
                    }

                    if (!String.IsNullOrEmpty(RemoteAddresses))
                    {
                        firewallRule.RemoteAddresses = RemoteAddresses;

                        if (!isTemp)
                        {
                            firewallRule.Name += " [T:" + RemoteAddresses + "]";
                        }
                    }

                    if (!String.IsNullOrEmpty(RemotePorts))
                    {
                        firewallRule.RemotePorts = RemotePorts;

                        if (!isTemp)
                        {
                            firewallRule.Name += " [R:" + RemotePorts + "]";
                        }
                    }

                    LogHelper.Debug("Adding rule to firewall...");
                    firewallPolicy.Rules.Add(firewallRule);

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

            public CustomRule(string ruleName, string currentPath, string currentAppPkgId, string localUserOwner, string[] services, int protocol, string target, string targetPort, string localport, int profiles, string action) : this(ruleName, currentPath, currentAppPkgId, localUserOwner, services != null ? String.Join(",", services) : null, protocol, target, targetPort, localport, profiles, action)
            {
                //Chained to the constructor below!
            }

            public CustomRule(string ruleName, string currentPath, string currentAppPkgId, string localUserOwner, string services, int protocol, string target, string targetPort, string localport, int profiles, string action)
            {
                Name = ruleName;
                ApplicationName = currentPath;
                AppPkgId = currentAppPkgId;
                LUOwn = localUserOwner;
                ServiceName = services != null ? String.Join(",", services) : null;
                Protocol = protocol;
                RemoteAddresses = target;
                RemotePorts = targetPort;
                LocalPorts = localport;
                Profiles = profiles;
                switch (action)
                {
                    case "A":
                        Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                        break;

                    case "B":
                        Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
                        break;

                    default:
                        throw new Exception("Unknown action type: " + action.ToString());
                }
            }
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

                return true;
            }
            catch (UnauthorizedAccessException)
            {
                //Don't have enough permissions
                ;
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

        public static string getType(int type)
        {
            return Enum.GetName(typeof(NET_FW_PROFILE_TYPE2_), type);
        }

        public static string getProtocolAsString(int protocol)
        {
            //These are the IANA protocol numbers.
            switch (protocol)
            {
                case 1:
                    return "ICMP";

                case 2:
                    return "IGMP"; //Used by OpenVPN, for example.

                case (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP: //6
                    return "TCP";

                case (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP: //17
                    return "UDP";

                case 47:
                    return "GRE"; //Used by PPTP, for example.

                case 58:
                    return "ICMPv6";

                case 136:
                    return "UDPLite";

                default:
                    LogHelper.Warning("Unknown protocol type: " + protocol.ToString());
                    return "Unknown";
            }
        }

        /// <summary>
        /// Converts the protocol integer to its NET_FW_IP_PROTOCOL_ representation.
        /// </summary>
        /// <returns></returns>
        public static int normalizeProtocol(int protocol)
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

        private static Rule[] wshRulesCache = null;

        public enum Protocols
        {
            TCP = NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP,
            UDP = NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP,
            ANY = NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY
        }

        public static Rule[] GetRules(bool AlsoGetInactive = false)
        {
            if (wshRulesCache == null)
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

        public static bool IsIPProtocol(int protocol)
        {
            //Used to check whether this protocol supports ports.
            return (protocol == (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP || protocol == (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP);
        }

        public static IEnumerable<Rule> GetMatchingRules(string path, string appPkgId, int protocol, string target, string targetPort, string localPort, IEnumerable<string> svc, string localUserOwner, bool blockOnly, bool outgoingOnly = true)
        {
            int currentProfile = GetCurrentProfile(); //This call is relatively slow, and calling it many times causes a startup delay. Let's cache it!
            IEnumerable<Rule> ret = GetRules().Where(r => RuleMatches(r, path, svc, protocol, localPort, target, targetPort, appPkgId, localUserOwner, currentProfile));
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


        private static bool RuleMatches(Rule r, string path, IEnumerable<string> svc, int protocol, string localport, string target, string remoteport, string appPkgId, string LocalUserOwner, int currentProfile)
        {
            //Note: This outputs *really* a lot, so use the if-statement to filter!
            /*if (!String.IsNullOrEmpty(r.ApplicationName) && r.ApplicationName.Contains("winword.exe"))
            {
                LogHelper.Debug(r.Enabled.ToString());
                LogHelper.Debug(r.Profiles.ToString() + " <--> " + currentProfile.ToString() + "   " + (((r.Profiles & currentProfile) != 0) || ((r.Profiles & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL) != 0)).ToString());
                LogHelper.Debug(r.ApplicationName + " <--> " + path + "   " + ((String.IsNullOrEmpty(r.ApplicationName) || StringComparer.CurrentCultureIgnoreCase.Compare(r.ApplicationName, path) == 0)).ToString());
                LogHelper.Debug(r.ServiceName + " <--> " + String.Join(",", svc) + "   " + ((String.IsNullOrEmpty(r.ServiceName) || (svc.Any() && (r.ServiceName == "*")) || svc.Contains(r.ServiceName, StringComparer.CurrentCultureIgnoreCase))).ToString());
                LogHelper.Debug(r.Protocol.ToString() + " <--> " + protocol.ToString() + "   " + (r.Protocol == (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY || r.Protocol == protocol).ToString());
                LogHelper.Debug(r.RemoteAddresses + " <--> " + target + "   " + CheckRuleAddresses(r.RemoteAddresses, target).ToString());
                LogHelper.Debug(r.RemotePorts + " <--> " + remoteport + "   " + CheckRulePorts(r.RemotePorts, remoteport).ToString());
                LogHelper.Debug(r.LocalPorts + " <--> " + localport + "   " + CheckRulePorts(r.LocalPorts, localport).ToString());
                LogHelper.Debug(r.AppPkgId + " <--> " + appPkgId + "   " + (String.IsNullOrEmpty(r.AppPkgId) || (r.AppPkgId == appPkgId)).ToString());
                LogHelper.Debug(r.LUOwn + " <--> " + LocalUserOwner + "   " + (String.IsNullOrEmpty(r.LUOwn) || (r.LUOwn == LocalUserOwner)).ToString());
            }*/
            bool ret = r.Enabled
                       && (((r.Profiles & currentProfile) != 0) || ((r.Profiles & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL) != 0))
                       && ((String.IsNullOrEmpty(r.ApplicationName) || StringComparer.CurrentCultureIgnoreCase.Compare(r.ApplicationName, path) == 0))
                       && ((String.IsNullOrEmpty(r.ServiceName) || (svc.Any() && (r.ServiceName == "*")) || svc.Contains(r.ServiceName, StringComparer.CurrentCultureIgnoreCase)))
                       && (r.Protocol == (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY || r.Protocol == protocol)
                       && CheckRuleAddresses(r.RemoteAddresses, target)
                       && CheckRulePorts(r.RemotePorts, remoteport)
                       && CheckRulePorts(r.LocalPorts, localport)
                       //&& r.EdgeTraversal == //@
                       //&& r.Interfaces == //@
                       //&& r.LocalAddresses //@
                       && (String.IsNullOrEmpty(r.AppPkgId) || (r.AppPkgId == appPkgId))
                       && (String.IsNullOrEmpty(r.LUOwn) || (r.LUOwn == LocalUserOwner))
                       ;

            return ret;
        }


        private static bool CheckRuleAddresses(string ruleAddresses, string checkedAddress)
        {
            if (String.IsNullOrEmpty(ruleAddresses) || ruleAddresses == "*")
            {
                return true;
            }
            if (!checkedAddress.Contains('/'))
            {
                checkedAddress += "/255.255.255.255";
            }
            foreach (string token in ruleAddresses.Split(','))
            {
                //FIXME: Handle:
                //FIXME: See: https://technet.microsoft.com/en-us/aa365366
                //"Defaultgateway"
                //"DHCP"
                //"DNS"
                //"WINS"
                //"LocalSubnet"
                if (token == checkedAddress)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool CheckRulePorts(string rulePorts, string checkedPort)
        {
            if (String.IsNullOrEmpty(rulePorts) || rulePorts == "*")
            {
                return true;
            }
            foreach (string token in rulePorts.Split(','))
            {
                if (token == checkedPort)
                {
                    return true;
                }
                //FIXME: Handle:
                //FIXME: See: https://msdn.microsoft.com/en-us/library/ff719847.aspx
                //RPC
                //RPC-EPMap
                //Teredo
                //IPHTTPSOut
                //IPHTTPSIn
                //Ply2Disc
                //FIXME: And: https://msdn.microsoft.com/en-us/library/cc231498.aspx
                //mDNS
                //CORTANA_OUT ?
                int checkedPortInt;
                if (checkedPort.Contains('-') && Int32.TryParse(checkedPort, out checkedPortInt))
                {
                    string[] portRange = checkedPort.Split(new Char[] { '-' }, 1);
                    if ((Int32.Parse(portRange[0]) >= checkedPortInt) && (checkedPortInt >= Int32.Parse(portRange[1])))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static string GetCurrentProfileAsText()
        {
            return GetProfileAsText(GetCurrentProfile());
        }

        public static string GetProfileAsText(int profile_type)
        {

            string[] ret = new string[3];
            int i = 0;
            if (profile_type == (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL)
            {
                ret[i] = Resources.FW_PROFILE_ALL;
            }
            else
            {
                if ((profile_type & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN) != 0)
                {
                    ret[i++] = Resources.FW_PROFILE_DOMAIN;
                }
                if ((profile_type & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE) != 0)
                {
                    ret[i++] = Resources.FW_PROFILE_PRIVATE;
                }
                if ((profile_type & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC) != 0)
                {
                    ret[i++] = Resources.FW_PROFILE_PUBLIC;
                }
            }
            return String.Join(", ", ret, 0, i);
        }

        public class FirewallStatusWrapper : INotifyPropertyChanged
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
            public bool PrivateIsOutAllowed { get; set; }
            public bool PrivateIsInAllowed { get; set; }
            public bool PrivateIsInBlockedNotif { get; set; }
            public bool PrivateIsOutBlockedNotif { get; set; }

            public bool PublicIsEnabled { get; set; }
            public bool PublicIsInBlocked { get; set; }
            public bool PublicIsOutBlocked { get; set; }
            public bool PublicIsOutAllowed { get; set; }
            public bool PublicIsInAllowed { get; set; }
            public bool PublicIsInBlockedNotif { get; set; }
            public bool PublicIsOutBlockedNotif { get; set; }

            public bool DomainIsEnabled { get; set; }
            public bool DomainIsInBlocked { get; set; }
            public bool DomainIsOutBlocked { get; set; }
            public bool DomainIsOutAllowed { get; set; }
            public bool DomainIsInAllowed { get; set; }
            public bool DomainIsInBlockedNotif { get; set; }
            public bool DomainIsOutBlockedNotif { get; set; }

            public bool AllIsEnabled
            {
                set
                {
                    PublicIsEnabled = value;
                    PrivateIsEnabled = value;
                    DomainIsEnabled = value;
                    OnPropertyChanged(nameof(PublicIsEnabled));
                    OnPropertyChanged(nameof(PrivateIsEnabled));
                    OnPropertyChanged(nameof(DomainIsEnabled));
                }
            }

            public bool AllIsInBlocked
            {
                set
                {
                    PublicIsInBlocked = value;
                    PrivateIsInBlocked = value;
                    DomainIsInBlocked = value;
                    OnPropertyChanged(nameof(PublicIsInBlocked));
                    OnPropertyChanged(nameof(PrivateIsInBlocked));
                    OnPropertyChanged(nameof(DomainIsInBlocked));
                }
            }
            
            public bool AllIsInAllowed
            {
                set
                {
                    PublicIsInAllowed = value;
                    PrivateIsInAllowed = value;
                    DomainIsInAllowed = value; 
                    OnPropertyChanged(nameof(PublicIsInAllowed));
                    OnPropertyChanged(nameof(PrivateIsInAllowed));
                    OnPropertyChanged(nameof(DomainIsInAllowed));
                }
            }

            public bool AllIsOutBlocked
            {
                set
                {
                    PublicIsOutBlocked = value;
                    PrivateIsOutBlocked = value;
                    DomainIsOutBlocked = value;
                    OnPropertyChanged(nameof(PublicIsOutBlocked));
                    OnPropertyChanged(nameof(PrivateIsOutBlocked));
                    OnPropertyChanged(nameof(DomainIsOutBlocked));
                }
            }

            public bool AllIsOutAllowed
            {
                set
                {
                    PublicIsOutAllowed = value;
                    PrivateIsOutAllowed = value;
                    DomainIsOutAllowed = value; 
                    OnPropertyChanged(nameof(PublicIsOutAllowed));
                    OnPropertyChanged(nameof(PrivateIsOutAllowed));
                    OnPropertyChanged(nameof(DomainIsOutAllowed));
                }
            }

            public bool AllIsInBlockedNotif
            {
                set
                {
                    PublicIsInBlockedNotif = value;
                    PrivateIsInBlockedNotif = value;
                    DomainIsInBlockedNotif = value;
                    OnPropertyChanged(nameof(PublicIsInBlockedNotif));
                    OnPropertyChanged(nameof(PrivateIsInBlockedNotif));
                    OnPropertyChanged(nameof(DomainIsInBlockedNotif));
                }
            }

            public bool AllIsOutBlockedNotif
            {
                set
                {
                    PublicIsOutBlockedNotif = value;
                    PrivateIsOutBlockedNotif = value;
                    DomainIsOutBlockedNotif = value;
                    OnPropertyChanged(nameof(PublicIsOutBlockedNotif));
                    OnPropertyChanged(nameof(PrivateIsOutBlockedNotif));
                    OnPropertyChanged(nameof(DomainIsOutBlockedNotif));
                }
            }

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
                PrivateIsOutAllowed = !PrivateIsOutBlocked && !PrivateIsOutBlockedNotif;

                PublicIsEnabled = (publicInStatus != Status.DISABLED);
                PublicIsInBlocked = (publicInStatus == Status.ENABLED_BLOCK);
                PublicIsInBlockedNotif = (publicInStatus == Status.ENABLED_NOTIFY);
                PublicIsOutBlocked = (publicOutStatus == Status.ENABLED_BLOCK);
                PublicIsOutAllowed = !PublicIsOutBlocked && !PublicIsOutBlockedNotif;

                DomainIsEnabled = (domainInStatus != Status.DISABLED);
                DomainIsInBlocked = (domainInStatus == Status.ENABLED_BLOCK);
                DomainIsInBlockedNotif = (domainInStatus == Status.ENABLED_NOTIFY);
                DomainIsOutBlocked = (domainOutStatus == Status.ENABLED_BLOCK);
                DomainIsOutAllowed = !DomainIsOutBlocked && !DomainIsOutBlockedNotif;
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

            public event PropertyChangedEventHandler PropertyChanged;

            [NotifyPropertyChangedInvocator]
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}