using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows.Media;
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
        /// 
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public static Owner GetOwner(int pid, int localPort)
        {
            var ret = GetAllConnections().FirstOrDefault(r => r.LocalPort == localPort && r.OwningPid == pid);
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
    }
}
