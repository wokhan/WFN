using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public class BaseHelper
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

        /// <summary>
        /// 
        /// </summary>
        public interface OWNER_MODULE
        {
            string RemoteAddress { get; }
            string LocalAddress { get; }

            int RemotePort { get; }
            int LocalPort { get; }

            Owner OwnerModule { get; }

            DateTime CreationTime { get; }

            uint OwningPid { get; }

            BaseHelper.MIB_TCP_STATE State { get; }
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
        public static Owner GetOwner(NetFwTypeLib.NET_FW_IP_PROTOCOL_ protocol, int localPort)
        {
            Owner ret = null;
            try
            {
                if (protocol == NetFwTypeLib.NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP)
                {
                    try
                    {
                        ret = IPHelpers.UDPHelper.GetAllUDPConnections().First(r => r.LocalPort == localPort)// && r.RemoteAddress == remoteAddr && r.RemotePort == remotePort)
                                                    .OwnerModule;
                    }
                    catch
                    {
                        if (Socket.OSSupportsIPv6)
                        {
                            ret = IPHelpers.UDP6Helper.GetAllUDP6Connections().First(r => r.LocalPort == localPort)// && r.RemoteAddress == remoteAddr && r.RemotePort == remotePort)
                                                         .OwnerModule;
                        }
                    }
                }
                else
                {
                    try
                    {
                        ret = IPHelpers.TCPHelper.GetAllTCPConnections().First(r => r.LocalPort == localPort)// && r.RemoteAddress == remoteAddr && r.RemotePort == remotePort)
                                                    .OwnerModule;
                    }
                    catch (Exception e)
                    {
                        if (Socket.OSSupportsIPv6)
                        {
                            ret = IPHelpers.TCP6Helper.GetAllTCP6Connections().First(r => r.LocalPort == localPort)// && r.RemoteAddress == remoteAddr && r.RemotePort == remotePort)
                                                         .OwnerModule;
                        }
                    }
                }

                return ret;
            }
            catch
            {
                return null;
            }
        }
    }
}
