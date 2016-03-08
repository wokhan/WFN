using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using Wokhan.WindowsFirewallNotifier.Common.Extensions;
using Wokhan.WindowsFirewallNotifier.Common.Helpers.IPHelpers;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public class IPHelper
    {

        #region Enums


        public enum TCPIP_OWNER_MODULE_INFO_CLASS
        {
            TCPIP_OWNER_MODULE_INFO_BASIC
        }

        public enum MIB_TCP_STATE
        {
            CLOSED = 1,
            LISTENING,
            SYN_SENT,
            SYN_RCVD,
            ESTABLISHED,
            FIN_WAIT1,
            FIN_WAIT2,
            CLOSE_WAIT,
            CLOSING,
            LAST_ACK,
            TIME_WAIT,
            DELETE_TCB,
            NOT_APPLICABLE = 65535
        }


        public enum AF_INET
        {
            IP4 = 2,
            IP6 = 23
        }

        #endregion


        #region Common structs

        public interface I_OWNER_MODULE
        {
            // CHANGE
            byte[] RemoteAddrBytes { get; }

            string RemoteAddress { get; }
            int RemotePort { get; }
            string LocalAddress { get; }
            int LocalPort { get; }
            Owner OwnerModule { get; }
            string Protocol { get; }
            DateTime? CreationTime { get; }
            uint OwningPid { get; }
            MIB_TCP_STATE State { get; }
            bool IsLoopback { get; }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TCPIP_OWNER_MODULE_BASIC_INFO
        {
            public IntPtr p1;
            public IntPtr p2;
        }

        public class Owner
        {
            public string ModuleName;
            public string ModulePath;

            private ImageSource _icon = null;
            public ImageSource Icon { get { return _icon = _icon ?? ProcessHelper.GetCachedIcon(ModulePath, true); } }

            public Owner(TCPIP_OWNER_MODULE_BASIC_INFO inf)
            {
                ModuleName = Marshal.PtrToStringAuto(inf.p1);
                ModulePath = Marshal.PtrToStringAuto(inf.p2);
            }
        }

        #endregion

        internal static string GetAddressAsString(byte[] _remoteAddr)
        {
            return _remoteAddr[0] + "." + _remoteAddr[1] + "." + _remoteAddr[2] + "." + _remoteAddr[3];
        }

        internal static int GetRealPort(byte[] _remotePort)
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(new byte[] { _remotePort[2], _remotePort[3], _remotePort[0], _remotePort[1] }, 0));
        }

        internal static string GetRealAddress(byte[] _remoteAddress)
        {
            return new IPAddress(_remoteAddress).ToString();
        }


        /// <summary>
        /// Returns details about connection of localPort by process identified by pid.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public static Owner GetOwner(int pid, int localPort)
        {
            var allConn = GetAllConnections();
            var ret = allConn.FirstOrDefault(r => r.LocalPort == localPort && r.OwningPid == pid);
            return ret != null ? ret.OwnerModule : null;
        }

        private static bool IsIPV6(string localAddress)
        {
            throw new NotImplementedException();
        }

        public static IEnumerable<I_OWNER_MODULE> GetAllConnections(bool tcpOnly = false)
        {
            var ret = TCPHelper.GetAllTCPConnections();
            if (!tcpOnly)
            {
                ret = ret.Concat(UDPHelper.GetAllUDPConnections());
            }

            if (Socket.OSSupportsIPv6)
            {
                ret = ret.Concat(TCP6Helper.GetAllTCP6Connections());
                if (!tcpOnly)
                {
                    ret = ret.Concat(UDP6Helper.GetAllUDP6Connections());
                }
            }

            return ret;
        }

        public static IPAddress GetPublicIpAddress()
        {
            var request = (HttpWebRequest)WebRequest.Create("http://checkip.eurodyndns.org/");
            request.Method = "GET";
            request.UserAgent = "curl";
            try
            {
                using (WebResponse response = request.GetResponse())
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        var ans = reader.ReadLines().Skip(2).First();
                        var adr = Regex.Match(ans, "Current IP Address: (.*)", RegexOptions.Singleline);

                        return IPAddress.Parse(adr.Groups[1].Value.Trim());
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public static async Task<IEnumerable<IPAddress>> GetFullRoute(string adr)
        {
            Ping pong = new Ping();
            PingOptions po = new PingOptions(1, true);
            List<IPAddress> ret = new List<IPAddress>();
            PingReply r = null;
            for (int i = 1; i < 30; i++)
            {
                if (r != null && r.Status != IPStatus.TimedOut)
                {
                    po.Ttl = i;
                }
                r = await pong.SendPingAsync(adr, 4000, new byte[32], po);

                if (r.Status == IPStatus.TtlExpired)
                {
                    ret.Add(r.Address);
                }
                else
                {
                    break;
                }
            }
            ret.Add(IPAddress.Parse(adr));
            return ret;
        }

    }
}
