using System;
using NetFwTypeLib;
using System.Linq;
using Wokhan.WindowsFirewallNotifier.Common.IO.Files;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Common.Core.Resources;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.WFP
{
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
            version = new Version(parts[0].Substring(1));
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

        public override string ApplicationName => PathResolver.GetFriendlyPath(parsed["app"].FirstOrDefault());

        public override string ApplicationShortName => System.IO.Path.GetFileName(ApplicationName);


        public override string AppPkgId => parsed["apppkgid"].FirstOrDefault();

        public override string Description => parsed["desc"].FirstOrDefault();

        public override NET_FW_RULE_DIRECTION_ Direction
        {
            get
            {
                return parsed["dir"].FirstOrDefault() == "In" ? NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN : NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
            }
        }

        public override bool EdgeTraversal => true; //FIXME: !

        public override int EdgeTraversalOptions => 0; //FIXME: !

        public override bool Enabled => parsed.Contains("active") ? bool.Parse(parsed["active"].FirstOrDefault()) : true;

        public override string Grouping => ""; //FIXME: !

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
                return string.Join(", ", parsed["la4"].Concat(parsed["la6"]).ToArray());
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
                return "WSH - " + ResourcesLoader.GetMSResourceString(parsed["name"].FirstOrDefault());
            }
        }

        public override int Profiles
        {
            get
            {
                if (!parsed["profile"].Any())
                {
                    return (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL;
                }
                var profiles = 0;
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
                return string.Join(", ", parsed["ra4"].Concat(parsed["ra6"]).ToArray());
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

        public override INetFwRule GetPreparedRule(bool isTemp)
        {
            INetFwRule firewallRule;

#pragma warning disable CS8600,CS8604
            if (!string.IsNullOrEmpty(AppPkgId))
            {
                //Need INetFwRule3
                firewallRule = (INetFwRule3)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
            }
            else
            {
                firewallRule = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
            }
#pragma warning restore CS8600,CS8604

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
    }
}