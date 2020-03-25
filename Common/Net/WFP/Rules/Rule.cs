using NetFwTypeLib;
using System.ComponentModel;
using System.Windows.Media;
using Wokhan.WindowsFirewallNotifier.Common.IO.Files;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Common.Properties;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.WFP
{
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

        private ImageSource? _icon = null;

        public event PropertyChangedEventHandler? PropertyChanged;

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
            Icon = await IconHelper.GetIconAsync(ApplicationName).ConfigureAwait(false); //FIXME: This is now expanded... Is that a problem?!?
        }

        public string ProfilesStr => getProfile(Profiles);

        public string ActionStr => getAction(Action);

        public string DirectionStr => getDirection(Direction);

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
            return GetProfileAsText(profile_type);
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

    }
}
