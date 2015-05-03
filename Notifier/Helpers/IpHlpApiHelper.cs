using System;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Net.Sockets;

namespace WindowsFirewallNotifier
{
    public static class IpHlpApiHelper
    {

        #region Enums

        public enum AF_INET
        {
            IP4 = 2,
            IP6 = 23
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

        public enum TCP_TABLE_CLASS
        {
            TCP_TABLE_BASIC_LISTENER,
            TCP_TABLE_BASIC_CONNECTIONS,
            TCP_TABLE_BASIC_ALL,
            TCP_TABLE_OWNER_PID_LISTENER,
            TCP_TABLE_OWNER_PID_CONNECTIONS,
            TCP_TABLE_OWNER_PID_ALL,
            TCP_TABLE_OWNER_MODULE_LISTENER,
            TCP_TABLE_OWNER_MODULE_CONNECTIONS,
            TCP_TABLE_OWNER_MODULE_ALL
        }

        public enum UDP_TABLE_CLASS
        {
            UDP_TABLE_BASIC,
            UDP_TABLE_OWNER_PID,
            UDP_TABLE_OWNER_MODULE
        }

        public enum TCPIP_OWNER_MODULE_INFO_CLASS
        {
            TCPIP_OWNER_MODULE_INFO_BASIC
        }

        #endregion

        #region IPV4

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetOwnerModuleFromTcpEntry(ref MIB_TCPROW_OWNER_MODULE pTcpEntry, TCPIP_OWNER_MODULE_INFO_CLASS Class, IntPtr Buffer, ref int pdwSize);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetOwnerModuleFromUdpEntry(ref MIB_UDPROW_OWNER_MODULE pUdpEntry, TCPIP_OWNER_MODULE_INFO_CLASS Class, IntPtr Buffer, ref int pdwSize);

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_TCPROW_OWNER_MODULE : OWNER_MODULE
        {
            public uint _state;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            byte[] _localAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            byte[] _localPort;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            byte[] _remoteAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            byte[] _remotePort;
            public uint _owningPid;
            long _creationTime;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public ulong[] OwningModuleInfo;

            public MIB_TCP_STATE State { get { return (MIB_TCP_STATE)_state; } }

            public string RemoteAddress { get { return GetAddressAsString(_remoteAddr); } }
            public string LocalAddress { get { return GetAddressAsString(_localAddr); } }

            public int RemotePort { get { return GetRealPort(_remotePort); } }
            public int LocalPort { get { return GetRealPort(_localPort); } }

            public Owner OwnerModule { get { return GetOwningModule(this); } }

            public uint OwningPid { get { return _owningPid; } }

            public DateTime CreationTime { get { return _creationTime == 0 ? DateTime.MinValue : DateTime.FromFileTime(_creationTime); } }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPTABLE_OWNER_MODULE
        {
            public uint NumEntries;
            public MIB_TCPROW_OWNER_MODULE FirstEntry;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_UDPROW_OWNER_MODULE : OWNER_MODULE
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]//, FieldOffset(0)]
            byte[] _localAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]//, FieldOffset(4)]
            byte[] _localPort;
            //[FieldOffset(8)]
            public uint _owningPid;
            //[FieldOffset(16)]
            long _creationTime;
            //[FieldOffset(24)]
            //public int SpecificPortBind;
            //[FieldOffset(24)]
            public int Flags;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] //FieldOffset(32), 
            public ulong[] OwningModuleInfo;

            public MIB_TCP_STATE State { get { return MIB_TCP_STATE.NOT_APPLICABLE; } }

            public uint OwningPid { get { return _owningPid; } }
            public string LocalAddress { get { return GetAddressAsString(_localAddr); } }

            public int LocalPort { get { return GetRealPort(_localPort); } }

            public Owner OwnerModule { get { return GetOwningModule(this); } }

            public DateTime CreationTime { get { return _creationTime == 0 ? DateTime.MinValue : DateTime.FromFileTime(_creationTime); } }

            public string RemoteAddress
            {
                get { return String.Empty; }
            }

            public int RemotePort
            {
                get { return -1; }
            }
        }


        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_UDPTABLE_OWNER_MODULE
        {
            public uint NumEntries;
            public MIB_UDPROW_OWNER_MODULE FirstEntry;
        }


        /// <summary>
        /// 
        /// </summary>
        public static MIB_TCPROW_OWNER_MODULE[] GetAllTCPConnections()
        {
            IntPtr buffTable = IntPtr.Zero;

            try
            {
                int buffSize = 0;
                GetExtendedTcpTable(IntPtr.Zero, ref buffSize, true, AF_INET.IP4, TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);

                buffTable = Marshal.AllocHGlobal(buffSize);
                MIB_TCPROW_OWNER_MODULE[] tTable;

                uint ret = GetExtendedTcpTable(buffTable, ref buffSize, true, AF_INET.IP4, TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);
                if (ret == 0)
                {
                    MIB_TCPTABLE_OWNER_MODULE tab = (MIB_TCPTABLE_OWNER_MODULE)Marshal.PtrToStructure(buffTable, typeof(MIB_TCPTABLE_OWNER_MODULE));
                    IntPtr rowPtr = (IntPtr)((long)buffTable + (long)Marshal.OffsetOf(typeof(MIB_TCPTABLE_OWNER_MODULE), "FirstEntry"));

                    tTable = new MIB_TCPROW_OWNER_MODULE[tab.NumEntries];
                    for (int i = 0; i < tab.NumEntries; i++)
                    {
                        tTable[i] = (MIB_TCPROW_OWNER_MODULE)Marshal.PtrToStructure(rowPtr, typeof(MIB_TCPROW_OWNER_MODULE));
                        rowPtr = (IntPtr)((long)rowPtr + (long)Marshal.SizeOf(tTable[i]));
                    }

                    return tTable;
                }
                else
                {
                    throw new Exception("Unable to retrieve all connections rows (err:" + ret + ")");
                }
            }
            catch (Exception e)
            {
                return null;
            }
            finally
            {
                if (buffTable != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffTable);
                }
            }
        }

        private static uint GetOwningModuleTCP(MIB_TCPROW_OWNER_MODULE row, ref IntPtr buffer)
        {
            int buffSize = 0;

            GetOwnerModuleFromTcpEntry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, IntPtr.Zero, ref buffSize);
            buffer = Marshal.AllocHGlobal(buffSize);
            return GetOwnerModuleFromTcpEntry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, buffer, ref buffSize);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static MIB_UDPROW_OWNER_MODULE[] GetAllUDPConnections()
        {
            IntPtr buffTable = IntPtr.Zero;

            try
            {
                int buffSize = 0;
                GetExtendedUdpTable(IntPtr.Zero, ref buffSize, true, AF_INET.IP4, UDP_TABLE_CLASS.UDP_TABLE_OWNER_MODULE, 0);

                buffTable = Marshal.AllocHGlobal(buffSize);
                MIB_UDPROW_OWNER_MODULE[] tTable;

                uint ret = GetExtendedUdpTable(buffTable, ref buffSize, true, AF_INET.IP4, UDP_TABLE_CLASS.UDP_TABLE_OWNER_MODULE, 0);
                if (ret == 0)
                {
                    MIB_UDPTABLE_OWNER_MODULE tab = (MIB_UDPTABLE_OWNER_MODULE)Marshal.PtrToStructure(buffTable, typeof(MIB_UDPTABLE_OWNER_MODULE));
                    IntPtr rowPtr = (IntPtr)((long)buffTable + (long)Marshal.OffsetOf(typeof(MIB_UDPTABLE_OWNER_MODULE), "FirstEntry"));

                    tTable = new MIB_UDPROW_OWNER_MODULE[tab.NumEntries];
                    for (int i = 0; i < tab.NumEntries; i++)
                    {
                        tTable[i] = (MIB_UDPROW_OWNER_MODULE)Marshal.PtrToStructure(rowPtr, typeof(MIB_UDPROW_OWNER_MODULE));
                        rowPtr = (IntPtr)((long)rowPtr + (long)Marshal.SizeOf(tTable[i]));
                    }

                    return tTable;
                }
                else
                {
                    throw new Exception("Unable to retrieve all connections rows (err:" + ret + ")");
                }
            }
            catch (Exception e)
            {
                return null;
            }
            finally
            {
                if (buffTable != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffTable);
                }
            }
        }

        private static uint GetOwningModuleUDP(MIB_UDPROW_OWNER_MODULE row, ref IntPtr buffer)
        {
            int buffSize = 0;
            GetOwnerModuleFromUdpEntry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, IntPtr.Zero, ref buffSize);
            buffer = Marshal.AllocHGlobal(buffSize);
            return GetOwnerModuleFromUdpEntry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, buffer, ref buffSize);
        }

        #endregion

        #region IPV6

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetOwnerModuleFromTcp6Entry(ref MIB_TCP6ROW_OWNER_MODULE pTcpEntry, TCPIP_OWNER_MODULE_INFO_CLASS Class, IntPtr Buffer, ref int pdwSize);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetOwnerModuleFromUdp6Entry(ref MIB_UDP6ROW_OWNER_MODULE pUdpEntry, TCPIP_OWNER_MODULE_INFO_CLASS Class, IntPtr Buffer, ref int pdwSize);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern void RtlIpv6AddressToString(byte[] Addr, out StringBuilder res);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct MIB_TCP6ROW_OWNER_MODULE : OWNER_MODULE
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] _localAddress;
            public uint LocalScopeId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            byte[] _localPort;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] _remoteAddress;
            public uint RemoteScopeId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            byte[] _remotePort;
            public uint _state;
            public uint _owningPid;
            long _creationTime;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public ulong[] OwningModuleInfo;

            public uint OwningPid { get { return _owningPid; } }
            public string LocalAddress { get { return GetRealAddress(_localAddress); } }
            public int LocalPort { get { return GetRealPort(_localPort); } }

            public string RemoteAddress { get { return GetRealAddress(_remoteAddress); } }
            public int RemotePort { get { return GetRealPort(_remotePort); } }

            public Owner OwnerModule { get { return GetOwningModule(this); } }

            public MIB_TCP_STATE State { get { return (IpHlpApiHelper.MIB_TCP_STATE)_state; } }
            public DateTime CreationTime { get { return _creationTime == 0 ? DateTime.MinValue : DateTime.FromFileTime(_creationTime); } }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCP6TABLE_OWNER_MODULE
        {
            public uint NumEntries;
            public MIB_TCP6ROW_OWNER_MODULE FirstEntry;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_UDP6ROW_OWNER_MODULE : OWNER_MODULE
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] _localAddress;
            public uint LocalScopeId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            byte[] _localPort;
            public uint _owningPid;
            //[FieldOffset(16)]
            long _creationTime;
            //[FieldOffset(24)]
            //public int SpecificPortBind;
            //[FieldOffset(24)]
            public int Flags;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] //FieldOffset(32), 
            public ulong[] OwningModuleInfo;

            public MIB_TCP_STATE State { get { return MIB_TCP_STATE.NOT_APPLICABLE; } }

            public uint OwningPid { get { return _owningPid; } }
            public string LocalAddress { get { return GetRealAddress(_localAddress); } }
            public int LocalPort { get { return GetRealPort(_localPort); } }
            public Owner OwnerModule { get { return GetOwningModule(this); } }
            public string RemoteAddress { get { return String.Empty; } }
            public int RemotePort { get { return -1; } }
            public DateTime CreationTime { get { return _creationTime == 0 ? DateTime.MinValue : DateTime.FromFileTime(_creationTime); } }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_UDP6TABLE_OWNER_MODULE
        {
            public uint NumEntries;
            public MIB_UDP6ROW_OWNER_MODULE FirstEntry;
        }

        /// <summary>
        /// 
        /// </summary>
        public static MIB_TCP6ROW_OWNER_MODULE[] GetAllTCP6Connections()
        {
            IntPtr buffTable = IntPtr.Zero;

            try
            {
                int buffSize = 0;
                GetExtendedTcpTable(IntPtr.Zero, ref buffSize, true, AF_INET.IP6, TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);

                buffTable = Marshal.AllocHGlobal(buffSize);
                MIB_TCP6ROW_OWNER_MODULE[] tTable;

                uint ret = GetExtendedTcpTable(buffTable, ref buffSize, true, AF_INET.IP6, TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);
                if (ret == 0)
                {
                    MIB_TCP6TABLE_OWNER_MODULE tab = (MIB_TCP6TABLE_OWNER_MODULE)Marshal.PtrToStructure(buffTable, typeof(MIB_TCP6TABLE_OWNER_MODULE));
                    IntPtr rowPtr = (IntPtr)((long)buffTable + (long)Marshal.OffsetOf(typeof(MIB_TCP6TABLE_OWNER_MODULE), "FirstEntry"));

                    tTable = new MIB_TCP6ROW_OWNER_MODULE[tab.NumEntries];
                    for (int i = 0; i < tab.NumEntries; i++)
                    {
                        tTable[i] = (MIB_TCP6ROW_OWNER_MODULE)Marshal.PtrToStructure(rowPtr, typeof(MIB_TCP6ROW_OWNER_MODULE));
                        rowPtr = (IntPtr)((long)rowPtr + (long)Marshal.SizeOf(tTable[i]));
                    }

                    return tTable;
                }
                else
                {
                    throw new Exception("Unable to retrieve all connections rows (err:" + ret + ")");
                }
            }
            catch (Exception e)
            {
                return null;
            }
            finally
            {
                if (buffTable != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffTable);
                }
            }
        }

        private static uint GetOwningModuleTCP(MIB_TCP6ROW_OWNER_MODULE row, ref IntPtr buffer)
        {
            int buffSize = 0;

            GetOwnerModuleFromTcp6Entry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, IntPtr.Zero, ref buffSize);
            buffer = Marshal.AllocHGlobal(buffSize);
            return GetOwnerModuleFromTcp6Entry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, buffer, ref buffSize);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static MIB_UDP6ROW_OWNER_MODULE[] GetAllUDP6Connections()
        {
            IntPtr buffTable = IntPtr.Zero;

            try
            {
                int buffSize = 0;
                GetExtendedUdpTable(IntPtr.Zero, ref buffSize, true, AF_INET.IP6, UDP_TABLE_CLASS.UDP_TABLE_OWNER_MODULE, 0);

                buffTable = Marshal.AllocHGlobal(buffSize);
                MIB_UDP6ROW_OWNER_MODULE[] tTable;

                uint ret = GetExtendedUdpTable(buffTable, ref buffSize, true, AF_INET.IP6, UDP_TABLE_CLASS.UDP_TABLE_OWNER_MODULE, 0);
                if (ret == 0)
                {
                    MIB_UDP6TABLE_OWNER_MODULE tab = (MIB_UDP6TABLE_OWNER_MODULE)Marshal.PtrToStructure(buffTable, typeof(MIB_UDP6TABLE_OWNER_MODULE));
                    IntPtr rowPtr = (IntPtr)((long)buffTable + (long)Marshal.OffsetOf(typeof(MIB_UDP6TABLE_OWNER_MODULE), "FirstEntry"));

                    tTable = new MIB_UDP6ROW_OWNER_MODULE[tab.NumEntries];
                    for (int i = 0; i < tab.NumEntries; i++)
                    {
                        tTable[i] = (MIB_UDP6ROW_OWNER_MODULE)Marshal.PtrToStructure(rowPtr, typeof(MIB_UDP6ROW_OWNER_MODULE));
                        rowPtr = (IntPtr)((long)rowPtr + (long)Marshal.SizeOf(tTable[i]));
                    }

                    return tTable;
                }
                else
                {
                    throw new Exception("Unable to retrieve all connections rows (err:" + ret + ")");
                }
            }
            catch (Exception e)
            {
                return null;
            }
            finally
            {
                if (buffTable != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffTable);
                }
            }
        }


        private static uint GetOwningModuleUDP(MIB_UDP6ROW_OWNER_MODULE row, ref IntPtr buffer)
        {
            int buffSize = 0;
            GetOwnerModuleFromUdp6Entry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, IntPtr.Zero, ref buffSize);
            buffer = Marshal.AllocHGlobal(buffSize);
            return GetOwnerModuleFromUdp6Entry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, buffer, ref buffSize);
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

            IpHlpApiHelper.MIB_TCP_STATE State { get; }
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

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int dwOutBufLen, bool sort, AF_INET ipVersion, TCP_TABLE_CLASS tblClass, int reserved);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref int dwOutBufLen, bool sort, AF_INET ipVersion, UDP_TABLE_CLASS tblClass, int reserved);

        private static string GetAddressAsString(byte[] _remoteAddr)
        {
            return _remoteAddr[0] + "." + _remoteAddr[1] + "." + _remoteAddr[2] + "." + _remoteAddr[3];
        }

        private static int GetRealPort(byte[] _remotePort)
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(new byte[] { _remotePort[2], _remotePort[3], _remotePort[0], _remotePort[1] }, 0));
        }

        private static string GetRealAddress(byte[] _remoteAddress)
        {
            return new IPAddress(_remoteAddress).ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="row"></param>
        /// <param name="GetOwnerModuleFunc"></param>
        /// <returns></returns>
        private static Owner GetOwningModule(OWNER_MODULE row)
        {
            IntPtr buffer = IntPtr.Zero;
            try
            {
                uint resp;
                if (row is MIB_TCPROW_OWNER_MODULE)
                {
                    resp = GetOwningModuleTCP((MIB_TCPROW_OWNER_MODULE)row, ref buffer);
                }
                else if (row is MIB_TCP6ROW_OWNER_MODULE)
                {
                    resp = GetOwningModuleTCP((MIB_TCP6ROW_OWNER_MODULE)row, ref buffer);
                }
                else if (row is MIB_UDPROW_OWNER_MODULE)
                {
                    resp = GetOwningModuleUDP((MIB_UDPROW_OWNER_MODULE)row, ref buffer);
                }
                else
                {
                    resp = GetOwningModuleUDP((MIB_UDP6ROW_OWNER_MODULE)row, ref buffer);
                }

                if (resp == 0)
                {
                    return new Owner((TCPIP_OWNER_MODULE_BASIC_INFO)Marshal.PtrToStructure(buffer, typeof(TCPIP_OWNER_MODULE_BASIC_INFO)));
                }
                else
                {
                    if (resp != 1168) // Ignore closed connections 
                    {
                        LogHelper.Error("Unable to get the connection owner.", new Exception("GetOwningModule returned " + resp));
                    }
                    return null;
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to get the connection owner.", e);
                return null;
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
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
                        ret = GetAllUDPConnections().First(r => r.LocalPort == localPort)// && r.RemoteAddress == remoteAddr && r.RemotePort == remotePort)
                                                    .OwnerModule;
                    }
                    catch
                    {
                        if (Socket.OSSupportsIPv6)
                        {
                            ret = GetAllUDP6Connections().First(r => r.LocalPort == localPort)// && r.RemoteAddress == remoteAddr && r.RemotePort == remotePort)
                                                         .OwnerModule;
                        }
                    }
                }
                else
                {
                    try
                    {
                        ret = GetAllTCPConnections().First(r => r.LocalPort == localPort)// && r.RemoteAddress == remoteAddr && r.RemotePort == remotePort)
                                                    .OwnerModule;
                    }
                    catch (Exception e)
                    {
                        if (Socket.OSSupportsIPv6)
                        {
                            ret = GetAllTCP6Connections().First(r => r.LocalPort == localPort)// && r.RemoteAddress == remoteAddr && r.RemotePort == remotePort)
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
