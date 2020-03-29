using Microsoft.Win32;
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
using Wokhan.WindowsFirewallNotifier.Common.IO.Streams;
using Wokhan.WindowsFirewallNotifier.Common.Net.IP.UDP;
using Wokhan.WindowsFirewallNotifier.Common.Net.IP.UDP6;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP
{
    public abstract partial class IPHelper
    {
        private const string MAX_USER_PORT_REGISTRY_KEY = "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters";
        private const string MAX_USER_PORT_REGISTRY_VALUE = "MaxUserPort";
        
        protected const uint NO_ERROR = 0;
        protected const uint ERROR_INSUFFICIENT_BUFFER = 122;
        protected const uint ERROR_NOT_FOUND = 1168;

        #region Enums

        [StructLayout(LayoutKind.Sequential)]
        internal struct TCPIP_OWNER_MODULE_BASIC_INFO
        {
            public IntPtr p1;
            public IntPtr p2;
        }

        internal enum TCPIP_OWNER_MODULE_INFO_CLASS
        {
            TCPIP_OWNER_MODULE_INFO_BASIC
        }

        internal enum AF_INET
        {
            IP4 = 2,
            IP6 = 23
        }

        #endregion

        internal static string GetAddressAsString(byte[] _remoteAddr)
        {
            return $"{_remoteAddr[0]}.{_remoteAddr[1]}.{_remoteAddr[2]}.{_remoteAddr[3]}";
        }

        internal static int GetRealPort(byte[] _remotePort)
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(new[] { _remotePort[2], _remotePort[3], _remotePort[0], _remotePort[1] }, 0));
        }

        internal static string GetRealAddress(byte[] _remoteAddress)
        {
            return new IPAddress(_remoteAddress).ToString();
        }

        public static string MergePorts(List<int> ports)
        {
            var result = "";
            var BeginRange = -2; //-2 to make sure it never matches any starting port (0 or larger).
            var EndRange = -2; //Initialization strictly speaking not necessary, but it shuts up a compiler warning.
            foreach (var port in ports)
            {
                if (port == EndRange + 1)
                {
                    //Part of the currently running range
                    EndRange = port;
                    continue;
                }
                else
                {
                    if (BeginRange != -2)
                    {
                        //Save the running range, because this port isn't part of it!
                        if (!String.IsNullOrEmpty(result))
                        {
                            result += ",";
                        }
                        if (BeginRange != EndRange)
                        {
                            //Actual range.
                            result += BeginRange.ToString() + "-" + EndRange.ToString();
                        }
                        else
                        {
                            //Lonely port.
                            result += BeginRange.ToString();
                        }
                    }
                    BeginRange = port;
                    EndRange = port;
                }
            }
            //Save the last running range, if any.
            if (BeginRange != -2)
            {
                //Save the running range, because this port isn't part of it!
                if (!String.IsNullOrEmpty(result))
                {
                    result += ",";
                }
                if (BeginRange != EndRange)
                {
                    //Actual range.
                    result += BeginRange.ToString() + "-" + EndRange.ToString();
                }
                else
                {
                    //Lonely port.
                    result += BeginRange.ToString();
                }
            }
            return result;
        }

        public static int GetMaxUserPort()
        {
            using var maxUserPortKey = Registry.LocalMachine.OpenSubKey(MAX_USER_PORT_REGISTRY_KEY, false);
            var maxUserPortValue = maxUserPortKey.GetValue(MAX_USER_PORT_REGISTRY_VALUE);
            if (maxUserPortValue is null)
            {
                //Default from Windows Vista and up
                return 49152;
            }

            return Convert.ToInt32(maxUserPortValue);
        }

        /// <summary>
        /// Returns details about connection of localPort by process identified by pid.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public static Owner? GetOwner(int pid, int localPort)
        {
            var allConn = GetAllConnections();
            var ret = allConn.FirstOrDefault(r => r.LocalPort == localPort && r.OwningPid == pid);
            return ret?.OwnerModule;
        }

        public static IEnumerable<IConnectionOwnerInfo> GetAllConnections(bool tcpOnly = false)
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
            var request = (HttpWebRequest)WebRequest.Create(new Uri("http://checkip.eurodyndns.org/"));
            request.Method = "GET";
            request.UserAgent = "curl";
            try
            {
                using WebResponse response = request.GetResponse();
                using var reader = new StreamReader(response.GetResponseStream());

                var ans = reader.ReadLines().Skip(2).First();
                var adr = Regex.Match(ans, "Current IP Address: (.*)", RegexOptions.Singleline);

                return IPAddress.Parse(adr.Groups[1].Value.Trim());
            }
            catch
            {
                return IPAddress.None;
            }
        }

        private const int buffer_size = 32;
        private const int max_hops = 30;
        private const int ping_timeout = 4000;
        public static async Task<IEnumerable<IPAddress>> GetFullRoute(string adr)
        {
            var ret = new List<IPAddress>();
            using (var pong = new Ping())
            {
                var po = new PingOptions(1, true);
                PingReply? r = null;
                var buffer = new byte[buffer_size];
                for (var i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = 0;
                }
                for (var i = 1; i < max_hops; i++)
                {
                    if (r != null && r.Status != IPStatus.TimedOut)
                    {
                        po.Ttl = i;
                    }
                    r = await pong.SendPingAsync(adr, ping_timeout, buffer, po).ConfigureAwait(false);

                    if (r.Status == IPStatus.TtlExpired)
                    {
                        ret.Add(r.Address);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            ret.Add(IPAddress.Parse(adr));
            return ret;
        }
    }
}
