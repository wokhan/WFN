using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers.IPHelpers
{
    public class UDPHelper : IPHelper
    {
        [DllImport("kernel32.dll", EntryPoint = "RtlZeroMemory", SetLastError = false)]
        public static extern void ZeroMemory(IntPtr dest, uint size);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetOwnerModuleFromUdpEntry(ref MIB_UDPROW_OWNER_MODULE pUdpEntry, TCPIP_OWNER_MODULE_INFO_CLASS Class, IntPtr Buffer, ref uint pdwSize);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref int dwOutBufLen, bool sort, AF_INET ipVersion, UDP_TABLE_CLASS tblClass, int reserved);

        protected const uint NO_ERROR = 0;
        protected const uint ERROR_INSUFFICIENT_BUFFER = 122;

        public enum UDP_TABLE_CLASS
        {
            UDP_TABLE_BASIC,
            UDP_TABLE_OWNER_PID,
            UDP_TABLE_OWNER_MODULE
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_UDPROW_OWNER_MODULE : I_OWNER_MODULE
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]//, FieldOffset(0)]
            byte[] _localAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]//, FieldOffset(4)]
            byte[] _localPort;
            //[FieldOffset(8)]
            uint _owningPid;
            //[FieldOffset(16)]
            long _creationTime;
            //[FieldOffset(24)]
            //public int SpecificPortBind;
            //[FieldOffset(24)]
            int _flags;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] //FieldOffset(32), 
            ulong[] _owningModuleInfo;

            public byte[] RemoteAddrBytes { get { return new byte[0]; } }

            public MIB_TCP_STATE State { get { return MIB_TCP_STATE.NOT_APPLICABLE; } }
            public uint OwningPid { get { return _owningPid; } }
            public string LocalAddress { get { return GetAddressAsString(_localAddr); } }
            public int LocalPort { get { return GetRealPort(_localPort); } }
            public Owner OwnerModule { get { return GetOwningModuleUDP(this); } }
            public string Protocol { get { return "UDP"; } }
            public DateTime? CreationTime { get { return _creationTime == 0 ? (DateTime?)null : DateTime.FromFileTime(_creationTime); } }
            public string RemoteAddress { get { return String.Empty; } }
            public int RemotePort { get { return -1; } }
            public bool IsLoopback { get { return IPAddress.IsLoopback(IPAddress.Parse(RemoteAddress)); } }
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
        /// <returns></returns>
        public static IEnumerable<I_OWNER_MODULE> GetAllUDPConnections()
        {
            IntPtr buffTable = IntPtr.Zero;

            try
            {
                int buffSize = 0;
                GetExtendedUdpTable(IntPtr.Zero, ref buffSize, false, AF_INET.IP4, UDP_TABLE_CLASS.UDP_TABLE_OWNER_MODULE, 0);

                buffTable = Marshal.AllocHGlobal(buffSize);

                uint ret = GetExtendedUdpTable(buffTable, ref buffSize, false, AF_INET.IP4, UDP_TABLE_CLASS.UDP_TABLE_OWNER_MODULE, 0);
                if (ret == 0)
                {
                    MIB_UDPTABLE_OWNER_MODULE tab = (MIB_UDPTABLE_OWNER_MODULE)Marshal.PtrToStructure(buffTable, typeof(MIB_UDPTABLE_OWNER_MODULE));
                    IntPtr rowPtr = (IntPtr)((long)buffTable + (long)Marshal.OffsetOf(typeof(MIB_UDPTABLE_OWNER_MODULE), "FirstEntry"));

                    MIB_UDPROW_OWNER_MODULE current;
                    for (int i = 0; i < tab.NumEntries; i++)
                    {
                        current = (MIB_UDPROW_OWNER_MODULE)Marshal.PtrToStructure(rowPtr, typeof(MIB_UDPROW_OWNER_MODULE));
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

        private static Dictionary<MIB_UDPROW_OWNER_MODULE, Owner> ownerCache = new Dictionary<MIB_UDPROW_OWNER_MODULE, Owner>();
        internal static Owner GetOwningModuleUDP(MIB_UDPROW_OWNER_MODULE row)
        {
            Owner ret = null;
            //if (ownerCache.TryGetValue(row, out ret))
            //{
            //    return ret;
            //}

            IntPtr buffer = IntPtr.Zero;
            try
            {
                uint buffSize = 0;
                var retn = GetOwnerModuleFromUdpEntry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, IntPtr.Zero, ref buffSize);
                if ((retn != NO_ERROR) && (retn != ERROR_INSUFFICIENT_BUFFER))
                {
                    //Cannot get owning module for this connection
                    return ret;
                }
                if (buffSize == 0)
                {
                    //No buffer? Probably means we can't retrieve any information about this connection; skip it
                    return ret;
                }
                buffer = Marshal.AllocHGlobal((int)buffSize);

                //GetOwnerModuleFromUdpEntry might want the fields of TCPIP_OWNER_MODULE_INFO_BASIC to be NULL
                ZeroMemory(buffer, buffSize);

                var resp = GetOwnerModuleFromUdpEntry(ref row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, buffer, ref buffSize);
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
