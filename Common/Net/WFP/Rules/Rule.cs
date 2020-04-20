using NetFwTypeLib;
using System.ComponentModel;
using System.Windows.Media;
using Wokhan.WindowsFirewallNotifier.Common.IO.Files;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Common.Properties;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.WFP.Rules
{
    public abstract class Rule : INotifyPropertyChanged
    {
        //Based on [MS-FASP] FW_RULE:
        public abstract string Name { get; }
        public abstract string? Description { get; }
        public abstract int Profiles { get; }
        public abstract NET_FW_RULE_DIRECTION_ Direction { get; }
        public abstract int Protocol { get; }
        public abstract string? LocalPorts { get; } //(Protocol 6, 17)
        public abstract string? RemotePorts { get; } //(Protocol 6, 17)
        public abstract string? IcmpTypesAndCodes { get; } //(Protocol 1, 58)
        public abstract string? LocalAddresses { get; }
        public abstract string? RemoteAddresses { get; }
        public abstract object? Interfaces { get; }
        public abstract string? InterfaceTypes { get; }
        public abstract string? ApplicationName { get; }
        public abstract string? ApplicationShortName { get; }
        public abstract string? ServiceName { get; }
        public abstract NET_FW_ACTION_ Action { get; }
        public abstract bool Enabled { get; } //Flags & FW_RULE_FLAGS_ACTIVE
        public abstract bool EdgeTraversal { get; } //Flags & FW_RULE_FLAGS_ROUTEABLE_ADDRS_TRAVERSE
        public abstract string? Grouping { get; } //Really: EmbeddedContext

        //v2.10:
        public abstract int EdgeTraversalOptions { get; } //EdgeTraversal, Flags & FW_RULE_FLAGS_ROUTEABLE_ADDRS_TRAVERSE_DEFER_APP, Flags & FW_RULE_FLAGS_ROUTEABLE_ADDRS_TRAVERSE_DEFER_USER

        //v2.20:
        //public abstract string LUAuth { get; }
        public abstract string? AppPkgId { get; }
        public abstract string? LUOwn { get; }

        //v2.24:
        //public abstract string Security { get; }

        //FIXME: Need to parse: (RA42=) RmtIntrAnet

        private ImageSource? _icon = null;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ImageSource? Icon
        {
            get
            {
                if (_icon is null)
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
            Icon = await IconHelper.GetIconAsync(ApplicationName).ConfigureAwait(false); //FIXME: This is now expanded... Is that a problem?!?
        }

        public string ProfilesStr => GetProfileAsText(Profiles);

        public string ActionStr => GetAction(Action);

        public string DirectionStr => GetDirection(Direction);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        private static string GetDirection(NET_FW_RULE_DIRECTION_ direction)
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
        private static string GetAction(NET_FW_ACTION_ action)
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

        public static string GetProfileAsText(int profile_type)
        {

            var ret = new string[3];
            var i = 0;
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
            return string.Join(", ", ret, 0, i);
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

        public abstract INetFwRule GetPreparedRule(bool isTemp);

        /// <summary>
        /// Converts the protocol integer to its NET_FW_IP_PROTOCOL_ representation.
        /// </summary>
        /// <returns></returns>
        public static NET_FW_IP_PROTOCOL_ NormalizeProtocol(int protocol)
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

        internal bool Matches(string path, IEnumerable<string> svc, int protocol, string localport, string target, string remoteport, string appPkgId, string LocalUserOwner, int currentProfile)
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
            return Enabled
                     && ((Profiles & currentProfile) != 0 || (Profiles & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL) != 0)
                     && (string.IsNullOrEmpty(ApplicationName) || StringComparer.CurrentCultureIgnoreCase.Compare(ApplicationName, path) == 0)
                     && (string.IsNullOrEmpty(ServiceName) || svc.Any() && ServiceName == "*" || svc.Contains(ServiceName, StringComparer.CurrentCultureIgnoreCase))
                     && (Protocol == WFP.Protocol.ANY || Protocol == protocol)
                     && CheckRuleAddresses(RemoteAddresses, target)
                     && CheckRulePorts(RemotePorts, remoteport)
                     && CheckRulePorts(LocalPorts, localport)
                     //&& r.EdgeTraversal == //@
                     //&& r.Interfaces == //@
                     //&& r.LocalAddresses //@
                     && (string.IsNullOrEmpty(AppPkgId) || AppPkgId == appPkgId)
                     && (string.IsNullOrEmpty(LUOwn) || LUOwn == LocalUserOwner)
                     ;
        }


        public bool MatchesEvent(int currentProfile, string appPkgId, string svcName, string path, string target = "*", string remoteport = "*")
        {
            var friendlyPath = string.IsNullOrWhiteSpace(path) ? path : PathResolver.GetFriendlyPath(path);
            var ruleFriendlyPath = string.IsNullOrWhiteSpace(ApplicationName) ? ApplicationName : PathResolver.GetFriendlyPath(ApplicationName);
            var ret = Enabled
                       && ((Profiles & currentProfile) != 0 || (Profiles & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL) != 0)
                       && (string.IsNullOrEmpty(ruleFriendlyPath) || ruleFriendlyPath.Equals(friendlyPath, StringComparison.OrdinalIgnoreCase))
                       && CheckRuleAddresses(RemoteAddresses, target)
                       && CheckRulePorts(RemotePorts, remoteport)
                       && (string.IsNullOrEmpty(AppPkgId) || AppPkgId == appPkgId)
                       && (string.IsNullOrEmpty(ServiceName) || svcName.Any() && ServiceName == "*" || svcName.Equals(ServiceName, StringComparison.OrdinalIgnoreCase))
                       ;
            if (ret && LogHelper.IsDebugEnabled())
            {
                LogHelper.Debug("Found enabled " + ActionStr + " " + DirectionStr + " Rule '" + Name + "'");
                LogHelper.Debug("\t" + Profiles.ToString() + " <--> " + currentProfile.ToString() + " : " + ((Profiles & currentProfile) != 0 || (Profiles & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL) != 0).ToString());
                LogHelper.Debug("\t" + ruleFriendlyPath + " <--> " + friendlyPath + " : " + (string.IsNullOrEmpty(ruleFriendlyPath) || ruleFriendlyPath.Equals(friendlyPath, StringComparison.OrdinalIgnoreCase)).ToString());
                LogHelper.Debug("\t" + RemoteAddresses + " <--> " + target + " : " + CheckRuleAddresses(RemoteAddresses, target).ToString());
                LogHelper.Debug("\t" + RemotePorts + " <--> " + remoteport + " : " + CheckRulePorts(RemotePorts, remoteport).ToString());
                LogHelper.Debug("\t" + AppPkgId + " <--> " + appPkgId + "  : " + (string.IsNullOrEmpty(AppPkgId) || AppPkgId == appPkgId).ToString());
                LogHelper.Debug("\t" + ServiceName + " <--> " + svcName + " : " + (string.IsNullOrEmpty(ServiceName) || svcName.Equals(ServiceName, StringComparison.OrdinalIgnoreCase)).ToString());
            }
            return ret;
        }

        private static bool CheckRuleAddresses(string? ruleAddresses, string checkedAddress)
        {
            if (string.IsNullOrEmpty(ruleAddresses) || ruleAddresses == "*")
            {
                return true;
            }
            if (!checkedAddress.Contains('/'))
            {
                checkedAddress += "/255.255.255.255";
            }
            foreach (var token in ruleAddresses.Split(','))
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

        private static bool CheckRulePorts(string? rulePorts, string checkedPort)
        {
            if (string.IsNullOrEmpty(rulePorts) || rulePorts == "*")
            {
                return true;
            }
            foreach (var token in rulePorts.Split(','))
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
                if (checkedPort.Contains('-') && int.TryParse(checkedPort, out checkedPortInt))
                {
                    var portRange = checkedPort.Split(new char[] { '-' }, 1);
                    if (int.Parse(portRange[0]) >= checkedPortInt && checkedPortInt >= int.Parse(portRange[1]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }


    }
}
