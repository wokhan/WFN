using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers.IPHelpers
{
    public class UDP6Helper : UDPHelper
    {
        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetOwnerModuleFromUdp6Entry(ref MIB_UDP6ROW_OWNER_MODULE pUdpEntry, TCPIP_OWNER_MODULE_INFO_CLASS Class, IntPtr Buffer, ref int pdwSize);

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_UDP6ROW_OWNER_MODULE : I_OWNER_MODULE
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

            public byte[] RemoteAddrBytes { get { return new byte[0]; } }
            public MIB_TCP_STATE State { get { return MIB_TCP_STATE.NOT_APPLICABLE; } }
            public uint OwningPid { get { return _owningPid; } }
            public string LocalAddress { get { return GetRealAddress(_localAddress); } }
            public int LocalPort { get { return GetRealPort(_localPort); } }
            public Owner OwnerModule { get { return GetOwningModuleUDP6(this); } }
            public string Protocol { get { return "UDP"; } }
            public string RemoteAddress { get { return String.Empty; } }
            public int RemotePort { get { return -1; } }
            public DateTime? CreationTime { get { return _creationTime == 0 ? (DateTime?)null : DateTime.FromFileTime(_creationTime); } }
            public bool IsLoopback { get { return IPAddress.IsLoopback(IPAddress.Parse(RemoteAddress)); } }
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
        /// <returns></returns>
        public static IEnumerable<I_OWNER_MODULE> GetAllUDP6Connections()
        {
            IntPtr buffTable = IntPtr.Zero;

            try
            {
                int buffSize = 0;
                GetExtendedUdpTable(IntPtr.Zero, ref buffSize, true, AF_INET.IP6, UDP_TABLE_CLASS.UDP_TABLE_OWNER_MODULE, 0);

                buffTable = Marshal.AllocHGlobal(buffSize);

                uint ret = GetExtendedUdpTable(buffTable, ref buffSize, true, AF_INET.IP6, UDP_TABLE_CLASS.UDP_TABLE_OWNER_MODULE, 0);
                if (ret == 0)
                {
                    MIB_UDP6TABLE_OWNER_MODULE tab = (MIB_UDP6TABLE_OWNER_MODULE)Marshal.PtrToStructure(buffTable, typeof(MIB_UDP6TABLE_OWNER_MODULE));
                    IntPtr rowPtr = (IntPtr)((long)buffTable + (long)Marshal.OffsetOf(typeof(MIB_UDP6TABLE_OWNER_MODULE), "FirstEntry"));

                    MIB_UDP6ROW_OWNER_MODULE current;
                    for (int i = 0; i < tab.NumEntries; i++)
                    {
                        current = (MIB_UDP6ROW_OWNER_MODULE)Marshal.PtrToStructure(rowPtr, typeof(MIB_UDP6ROW_OWNER_MODULE));
                        rowPtr = (IntPtr)((long)rowPtr + (long)Marshal.SizeOf(current));

                        yield return current;
                    }
                }
                else
                {
                    throw new Win32Exception((int)ret);
                }
            }
            finally
            {
                if (buffTable != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffTable);
                }
            }
        }

        private static Dictionary<MIB_UDP6ROW_OWNER_MODULE, Owner> ownerCache = new Dictionary<MIB_UDP6ROW_OWNER_MODULE, Owner>();
        internal static Owner GetOwningModuleUDP6(MIB_UDP6ROW_OWNER_MODULE row)
        {
            Owner ret = null;
            //if (ownerCache.TryGetValue(row, out ret))
            //{
            //    return ret;
            //}

            IntPtr buffer = IntPtr.Zero;
            try
            {
                int buffSize = 0;
                GetOwnerModuleFromUdp6Entry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, IntPtr.Zero, ref buffSize);
                buffer = Marshal.AllocHGlobal(buffSize);

                var resp = GetOwnerModuleFromUdp6Entry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, buffer, ref buffSize);
                if (resp == 0)
                {
                    ret = new Owner((TCPIP_OWNER_MODULE_BASIC_INFO)Marshal.PtrToStructure(buffer, typeof(TCPIP_OWNER_MODULE_BASIC_INFO)));
                }
                else if (resp != 1168) // Ignore closed connections 
                {
                    LogHelper.Error("Unable to get the connection owner.", new Win32Exception((int)resp));
                }

                //ownerCache.Add(row, ret);

                return ret;
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
