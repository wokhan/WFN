using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers.IPHelpers
{
    public class TCP6Helper : TCPHelper
    {
        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetOwnerModuleFromTcp6Entry(ref MIB_TCP6ROW_OWNER_MODULE pTcpEntry, TCPIP_OWNER_MODULE_INFO_CLASS Class, IntPtr Buffer, ref int pdwSize);


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

            public MIB_TCP_STATE State { get { return (MIB_TCP_STATE)_state; } }
            public DateTime CreationTime { get { return _creationTime == 0 ? DateTime.MinValue : DateTime.FromFileTime(_creationTime); } }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCP6TABLE_OWNER_MODULE
        {
            public uint NumEntries;
            public MIB_TCP6ROW_OWNER_MODULE FirstEntry;
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

        internal static uint GetOwningModuleTCP(MIB_TCP6ROW_OWNER_MODULE row, ref IntPtr buffer)
        {
            int buffSize = 0;

            GetOwnerModuleFromTcp6Entry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, IntPtr.Zero, ref buffSize);
            buffer = Marshal.AllocHGlobal(buffSize);
            return GetOwnerModuleFromTcp6Entry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, buffer, ref buffSize);
        }


        internal static Owner GetOwningModule(OWNER_MODULE row)
        {
            IntPtr buffer = IntPtr.Zero;
            try
            {
                uint resp = IPHelpers.TCP6Helper.GetOwningModuleTCP((IPHelpers.TCP6Helper.MIB_TCP6ROW_OWNER_MODULE)row, ref buffer);
                
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
    }
}