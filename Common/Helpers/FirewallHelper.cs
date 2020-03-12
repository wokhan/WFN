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
using Wokhan.WindowsFirewallNotifier.Common.Extensions;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public static class FirewallHelper
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

        public abstract class Rule : INotifyPropertyChanged
        {
            //Based on [MS-FASP] FW_RULE:
            public abstract string Name { get; }
            public abstract string Description { get; }
            public abstract int Profiles { get; }
            public abstract NET_FW_RULE_DIRECTION_ Direction { get; }
            public abstract int Protocol { get; }
            public abstract string LocalPorts { get; } //(Protocol 6, 17)
            public abstract string RemotePorts { get; } //(Protocol 6, 17)
            public abstract string IcmpTypesAndCodes { get; } //(Protocol 1, 58)
            public abstract string LocalAddresses { get; }
            public abstract string RemoteAddresses { get; }
            public abstract object Interfaces { get; }
            public abstract string InterfaceTypes { get; }
            public abstract string ApplicationName { get; }
            public abstract string ApplicationShortName { get; }
            public abstract string ServiceName { get; }
            public abstract NET_FW_ACTION_ Action { get; }
            public abstract bool Enabled { get; } //Flags & FW_RULE_FLAGS_ACTIVE
            public abstract bool EdgeTraversal { get; } //Flags & FW_RULE_FLAGS_ROUTEABLE_ADDRS_TRAVERSE
            public abstract string Grouping { get; } //Really: EmbeddedContext

            //v2.10:
            public abstract int EdgeTraversalOptions { get; } //EdgeTraversal, Flags & FW_RULE_FLAGS_ROUTEABLE_ADDRS_TRAVERSE_DEFER_APP, Flags & FW_RULE_FLAGS_ROUTEABLE_ADDRS_TRAVERSE_DEFER_USER

            //v2.20:
            //public abstract string LUAuth { get; }
            public abstract string AppPkgId { get; }
            public abstract string LUOwn { get; }

            //v2.24:
            //public abstract string Security { get; }

            //FIXME: Need to parse: (RA42=) RmtIntrAnet

            private ImageSource _icon = null;

            public event PropertyChangedEventHandler PropertyChanged;

            public ImageSource Icon
            {
                get
                {
                    if (_icon == null)
                    {
                        UpdateIcon();
                    }

                    return _icon;
                }
                private set
                {
                    if (_icon != value)
                    {
                        _icon = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));
                    }
                }
            }

            private async void UpdateIcon()
            {
                Icon = await IconHelper.GetIconAsync(this.ApplicationName); //FIXME: This is now expanded... Is that a problem?!?
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

            //public bool ApplyIndirect(bool isTemp)
            //{
            //    string actionString;
            //    switch (Action)
            //    {
            //        case NET_FW_ACTION_.NET_FW_ACTION_ALLOW:
            //            actionString = "A";
            //            break;

            //        case NET_FW_ACTION_.NET_FW_ACTION_BLOCK:
            //            actionString = "B";
            //            break;

            //        default:
            //            throw new Exception("Unknown action type: " + Action.ToString());
            //    }
            //    if (isTemp)
            //    {
            //        actionString = "T";
            //    }
            //    string param = Convert.ToBase64String(Encoding.Unicode.GetBytes(String.Format(indParamFormat, Name, ApplicationName, AppPkgId, LUOwn, ServiceName, Protocol, RemoteAddresses, RemotePorts, LocalPorts, Profiles, actionString)));
            //    return ProcessHelper.getProcessFeedback(WFNRuleManagerEXE, args: param, runas: true, dontwait: isTemp);
            //}

            public abstract bool Apply(bool isTemp);
        }

        public class WSHRule : Rule
        {
            //Based on [MS-GPFAS] ABNF Grammar:
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
                    //@@@ "ByPass"
                    return parsed["action"].FirstOrDefault() == "Block" ? NET_FW_ACTION_.NET_FW_ACTION_BLOCK : NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                }
            }

            public override string ApplicationName
            {
                get
                {
                    return FileHelper.GetFriendlyPath(parsed["app"].FirstOrDefault());
                }
            }

            public override string ApplicationShortName
            {
                get
                {
                    return System.IO.Path.GetFileName(ApplicationName);
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
                        firewallRule.Protocol = (int)normalizeProtocol(Protocol);
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
            public override string ApplicationShortName
            {
                get
                {
                    return (ApplicationName != null ? System.IO.Path.GetFileName(ApplicationName) : String.Empty);
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
                        firewallRule.Protocol = (int)normalizeProtocol(Protocol);
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

            public CustomRule(string ruleName, string currentPath, string currentAppPkgId, string localUserOwner, string[] services, int protocol, string target, string targetPort, string localport
                , int profiles, CustomRuleAction action)
                : this(ruleName, currentPath, currentAppPkgId, localUserOwner, (services != null ? String.Join(",", services) : null), protocol, target, targetPort, localport, profiles, action)
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
                ServiceName = services != null ? String.Join(",", services) : null;
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

        public static string getEventInstanceIdAsString(long eventId)
        {
            // https://docs.microsoft.com/en-us/windows/security/threat-protection/auditing/audit-filtering-platform-connection
            string reason = "Block: {0} ";
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

        public static string getProtocolAsString(int protocol)
        {
            //These are the IANA protocol numbers.
            // Source: https://www.iana.org/assignments/protocol-numbers/protocol-numbers.xhtml
            // TODO: Add all the others and use an array?
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

                case 40:
                    return "IL"; // IL Transport protocol

                case 42:
                    return "SDRP"; // Source Demand Routing Protocol

                case 47:
                    return "GRE"; //Used by PPTP, for example.

                case 58:
                    return "ICMPv6";

                case 136:
                    return "UDPLite";

                case 36:
                    return "XTP";

                default:
                    LogHelper.Warning("Unknown protocol type: " + protocol.ToString());
                    return protocol >= 0 ? protocol.ToString() : "Unknown";
            }
        }

        /// <summary>
        /// Converts the protocol integer to its NET_FW_IP_PROTOCOL_ representation.
        /// </summary>
        /// <returns></returns>
        public static NET_FW_IP_PROTOCOL_ normalizeProtocol(int protocol)
        {
            try
            {
                return (NET_FW_IP_PROTOCOL_)protocol;
            }
            catch
            {
                return NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY;
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
            return r.Enabled
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
        }


        class SimpleEventRuleCompare : IEqualityComparer<FirewallHelper.Rule>
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
        public static IEnumerable<Rule> GetMatchingRulesForEvent(int pid, string path, string target, string targetPort, bool blockOnly = true, bool outgoingOnly = false)
        {
            String appPkgId = (pid > 0) ? ProcessHelper.getAppPkgId(pid) : String.Empty;
            int currentProfile = GetCurrentProfile();
            string svcName = "*";
            path = path ?? "";
            if (pid > 0 && path.EndsWith("svchost.exe", StringComparison.OrdinalIgnoreCase))
            {
                // get the scvName associated with svchost.exe
                string cLine = ProcessHelper.getCommandLineFromProcessWMI(pid);
                if (cLine != null)
                {
                    svcName = cLine.Split(new string[] { " -s " }, StringSplitOptions.None).Last().Split(' ').First();
                }
            }

            String exeName = System.IO.Path.GetFileName(path);
            LogHelper.Debug($"\nGetMatchingRulesForEvent: path={exeName}, svcName={svcName}, pid={pid}, target={target} targetPort={targetPort}, blockOnly={blockOnly}, outgoingOnly={outgoingOnly}");

            //IEnumerable<Rule> ret = GetRules(AlsoGetInactive: false).Distinct(new SimpleEventRuleCompare()).Where(r => r.Enabled && RuleMatchesEvent(r, currentProfile, appPkgId, svcName, path, target, targetPort));
            IEnumerable<Rule> ret = GetRules(AlsoGetInactive: false).Where(r =>
            {
                return RuleMatchesEvent(r, currentProfile, appPkgId, svcName, path, target, targetPort);
            });
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

        public static bool RuleMatchesEvent(Rule r, int currentProfile, string appPkgId, string svcName, string path, string target = "*", string remoteport = "*")
        {
            string friendlyPath = String.IsNullOrWhiteSpace(path) ? path : FileHelper.GetFriendlyPath(path);
            string ruleFriendlyPath = String.IsNullOrWhiteSpace(r.ApplicationName) ? r.ApplicationName : FileHelper.GetFriendlyPath(r.ApplicationName);
            bool ret = r.Enabled
                       && (((r.Profiles & currentProfile) != 0) || ((r.Profiles & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL) != 0))
                       && (String.IsNullOrEmpty(ruleFriendlyPath) || ruleFriendlyPath.Equals(friendlyPath, StringComparison.OrdinalIgnoreCase))
                       && CheckRuleAddresses(r.RemoteAddresses, target)
                       && CheckRulePorts(r.RemotePorts, remoteport)
                       && (String.IsNullOrEmpty(r.AppPkgId) || (r.AppPkgId == appPkgId))
                       && (String.IsNullOrEmpty(r.ServiceName) || (svcName.Any() && (r.ServiceName == "*")) || svcName.Equals(r.ServiceName, StringComparison.OrdinalIgnoreCase))
                       ;
            if (ret && LogHelper.isDebugEnabled())
            {
                LogHelper.Debug("Found enabled " + r.ActionStr + " " + r.DirectionStr + " Rule '" + r.Name + "'");
                LogHelper.Debug("\t" + r.Profiles.ToString() + " <--> " + currentProfile.ToString() + " : " + (((r.Profiles & currentProfile) != 0) || ((r.Profiles & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL) != 0)).ToString());
                LogHelper.Debug("\t" + ruleFriendlyPath + " <--> " + friendlyPath + " : " + ((String.IsNullOrEmpty(ruleFriendlyPath) || ruleFriendlyPath.Equals(friendlyPath, StringComparison.OrdinalIgnoreCase)).ToString()));
                LogHelper.Debug("\t" + r.RemoteAddresses + " <--> " + target + " : " + CheckRuleAddresses(r.RemoteAddresses, target).ToString());
                LogHelper.Debug("\t" + r.RemotePorts + " <--> " + remoteport + " : " + CheckRulePorts(r.RemotePorts, remoteport).ToString());
                LogHelper.Debug("\t" + r.AppPkgId + " <--> " + appPkgId + "  : " + (String.IsNullOrEmpty(r.AppPkgId) || (r.AppPkgId == appPkgId)).ToString());
                LogHelper.Debug("\t" + r.ServiceName + " <--> " + svcName + " : " + ((String.IsNullOrEmpty(r.ServiceName) || svcName.Equals(r.ServiceName, StringComparison.OrdinalIgnoreCase))).ToString());
            }
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
                //FW_ADDRESS_KEYWORD:
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
                //FW_PORT_KEYWORD:
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
            private bool _privateIsEnabled;
            private bool _privateIsInBlocked;
            private bool _privateIsOutBlocked;
            private bool _privateIsOutAllowed;
            private bool _privateIsInAllowed;
            private bool _privateIsInBlockedNotif;
            private bool _privateIsOutBlockedNotif;
            private bool _publicIsEnabled;
            private bool _publicIsInBlocked;
            private bool _publicIsOutBlocked;
            private bool _publicIsOutAllowed;
            private bool _publicIsInAllowed;
            private bool _publicIsInBlockedNotif;
            private bool _publicIsOutBlockedNotif;
            private bool _domainIsEnabled;
            private bool _domainIsInBlocked;
            private bool _domainIsOutBlocked;
            private bool _domainIsOutAllowed;
            private bool _domainIsInAllowed;
            private bool _domainIsInBlockedNotif;
            private bool _domainIsOutBlockedNotif;

            public bool PrivateIsEnabled
            {
                get => _privateIsEnabled;
                set
                {
                    if (value == _privateIsEnabled) return;
                    _privateIsEnabled = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsEnabled));
                }
            }

            public bool PrivateIsInBlocked
            {
                get => _privateIsInBlocked;
                set
                {
                    if (value == _privateIsInBlocked) return;
                    _privateIsInBlocked = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsInBlocked));
                }
            }

            public bool PrivateIsOutBlocked
            {
                get => _privateIsOutBlocked;
                set
                {
                    if (value == _privateIsOutBlocked) return;
                    _privateIsOutBlocked = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsOutBlocked));
                }
            }

            public bool PrivateIsOutAllowed
            {
                get => _privateIsOutAllowed;
                set
                {
                    if (value == _privateIsOutAllowed) return;
                    _privateIsOutAllowed = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsOutAllowed));
                }
            }

            public bool PrivateIsInAllowed
            {
                get => _privateIsInAllowed;
                set
                {
                    if (value == _privateIsInAllowed) return;
                    _privateIsInAllowed = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsInAllowed));
                }
            }

            public bool PrivateIsInBlockedNotif
            {
                get => _privateIsInBlockedNotif;
                set
                {
                    if (value == _privateIsInBlockedNotif) return;
                    _privateIsInBlockedNotif = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsInBlockedNotif));
                }
            }

            public bool PrivateIsOutBlockedNotif
            {
                get => _privateIsOutBlockedNotif;
                set
                {
                    if (value == _privateIsOutBlockedNotif) return;
                    _privateIsOutBlockedNotif = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsOutBlockedNotif));
                }
            }

            public bool PublicIsEnabled
            {
                get => _publicIsEnabled;
                set
                {
                    if (value == _publicIsEnabled) return;
                    _publicIsEnabled = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsEnabled));
                }
            }

            public bool PublicIsInBlocked
            {
                get => _publicIsInBlocked;
                set
                {
                    if (value == _publicIsInBlocked) return;
                    _publicIsInBlocked = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsInBlocked));
                }
            }

            public bool PublicIsOutBlocked
            {
                get => _publicIsOutBlocked;
                set
                {
                    if (value == _publicIsOutBlocked) return;
                    _publicIsOutBlocked = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsOutBlocked));
                }
            }

            public bool PublicIsOutAllowed
            {
                get => _publicIsOutAllowed;
                set
                {
                    if (value == _publicIsOutAllowed) return;
                    _publicIsOutAllowed = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsOutAllowed));
                }
            }

            public bool PublicIsInAllowed
            {
                get => _publicIsInAllowed;
                set
                {
                    if (value == _publicIsInAllowed) return;
                    _publicIsInAllowed = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsInAllowed));
                }
            }

            public bool PublicIsInBlockedNotif
            {
                get => _publicIsInBlockedNotif;
                set
                {
                    if (value == _publicIsInBlockedNotif) return;
                    _publicIsInBlockedNotif = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsInBlockedNotif));
                }
            }

            public bool PublicIsOutBlockedNotif
            {
                get => _publicIsOutBlockedNotif;
                set
                {
                    if (value == _publicIsOutBlockedNotif) return;
                    _publicIsOutBlockedNotif = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsOutBlockedNotif));
                }
            }

            public bool DomainIsEnabled
            {
                get => _domainIsEnabled;
                set
                {
                    if (value == _domainIsEnabled) return;
                    _domainIsEnabled = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsEnabled));
                }
            }

            public bool DomainIsInBlocked
            {
                get => _domainIsInBlocked;
                set
                {
                    if (value == _domainIsInBlocked) return;
                    _domainIsInBlocked = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsInBlocked));
                }
            }

            public bool DomainIsOutBlocked
            {
                get => _domainIsOutBlocked;
                set
                {
                    if (value == _domainIsOutBlocked) return;
                    _domainIsOutBlocked = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsOutBlocked));
                }
            }

            public bool DomainIsOutAllowed
            {
                get => _domainIsOutAllowed;
                set
                {
                    if (value == _domainIsOutAllowed) return;
                    _domainIsOutAllowed = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsOutAllowed));
                }
            }

            public bool DomainIsInAllowed
            {
                get => _domainIsInAllowed;
                set
                {
                    if (value == _domainIsInAllowed) return;
                    _domainIsInAllowed = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsInAllowed));
                }
            }

            public bool DomainIsInBlockedNotif
            {
                get => _domainIsInBlockedNotif;
                set
                {
                    if (value == _domainIsInBlockedNotif) return;
                    _domainIsInBlockedNotif = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsInBlockedNotif));
                }
            }

            public bool DomainIsOutBlockedNotif
            {
                get => _domainIsOutBlockedNotif;
                set
                {
                    if (value == _domainIsOutBlockedNotif) return;
                    _domainIsOutBlockedNotif = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AllIsOutBlockedNotif));
                }
            }

            public bool? AllIsEnabled
            {
                get
                {
                    if (PublicIsEnabled == PrivateIsEnabled && PrivateIsEnabled == DomainIsEnabled)
                    {
                        return PublicIsEnabled;
                    }

                    return null;
                }
                set
                {
                    if (value != null)
                    {
                        var bValue = value.Value;
                        PublicIsEnabled = bValue;
                        PrivateIsEnabled = bValue;
                        DomainIsEnabled = bValue;
                        OnPropertyChanged(nameof(PublicIsEnabled));
                        OnPropertyChanged(nameof(PrivateIsEnabled));
                        OnPropertyChanged(nameof(DomainIsEnabled));
                    }
                }
            }

            public bool AllIsInBlocked
            {
                get => PublicIsInBlocked && PrivateIsInBlocked && DomainIsInBlocked;
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
                get => PublicIsInAllowed && PrivateIsInAllowed && DomainIsInAllowed;
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
                get => PublicIsOutBlocked && PrivateIsOutBlocked && DomainIsOutBlocked;
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
                get => PublicIsOutAllowed && PrivateIsOutAllowed && DomainIsOutAllowed;
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
                get => PublicIsInBlockedNotif && PrivateIsInBlockedNotif && DomainIsInBlockedNotif;
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
                get => PublicIsOutBlockedNotif && PrivateIsOutBlockedNotif && DomainIsOutBlockedNotif;
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