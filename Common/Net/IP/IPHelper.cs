using Microsoft.Win32;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Windows.Win32;
using Windows.Win32.NetworkManagement.IpHelper;


namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP;

public abstract partial class IPHelper
{
    private const string MAX_USER_PORT_REGISTRY_KEY = "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters";
    private const string MAX_USER_PORT_REGISTRY_VALUE = "MaxUserPort";

    protected const uint NO_ERROR = 0;
    protected const uint ERROR_INSUFFICIENT_BUFFER = 122;
    internal const uint ERROR_NOT_FOUND = 1168;

    internal static readonly TCP_ESTATS_BANDWIDTH_ROD_v0 NoBandwidth = new TCP_ESTATS_BANDWIDTH_ROD_v0();

    public static event EventHandler<PropertyChangedEventArgs>? StaticPropertyChanged;

    internal static int GetRealPort(uint _remotePort)
    {
        // This is not working as expected
        //return IPAddress.NetworkToHostOrder((ushort)_remotePort);
        // While this is. Which is fun since this is the code of NetworkToHostOrder...
        return (int)(BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness((ushort)_remotePort) : (ushort)_remotePort);
    }

    internal static Dictionary<Connection, MIB_TCPROW_LH> TCP4MIBCACHE = [];

    public static string MergePorts(IEnumerable<int> ports)
    {
        var result = "";
        var BeginRange = -2; // -2 to make sure it never matches any starting port (0 or larger).
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

    private static int maxUserPort = -1;
    public static int GetMaxUserPort()
    {
        if (maxUserPort == -1)
        {
            using var maxUserPortKey = Registry.LocalMachine.OpenSubKey(MAX_USER_PORT_REGISTRY_KEY, false);
            var maxUserPortValue = maxUserPortKey?.GetValue(MAX_USER_PORT_REGISTRY_VALUE);
            if (maxUserPortValue is null)
            {
                //Default from Windows Vista and up
                maxUserPort = 49152;
            }

            maxUserPort = Convert.ToInt32(maxUserPortValue);
        }
        return maxUserPort;
    }

    public static IEnumerable<Connection> GetAllConnections(bool tcpOnly = false)
    {
        var ret = GetAllTCPConnections<MIB_TCPTABLE_OWNER_MODULE, MIB_TCPROW_OWNER_MODULE>(AF_INET.IP4).Select(tcpConn => new Connection(tcpConn));
        if (!tcpOnly)
        {
            ret = ret.Concat(GetAllUDPConnections<MIB_UDPTABLE_OWNER_MODULE, MIB_UDPROW_OWNER_MODULE>(AF_INET.IP4).Select(tcpConn => new Connection(tcpConn)));
        }

        if (Socket.OSSupportsIPv6)
        {
            ret = ret.Concat(GetAllTCPConnections<MIB_TCP6TABLE_OWNER_MODULE, MIB_TCP6ROW_OWNER_MODULE>(AF_INET.IP6).Select(tcpConn => new Connection(tcpConn)));
            if (!tcpOnly)
            {
                ret = ret.Concat(GetAllUDPConnections<MIB_UDP6TABLE_OWNER_MODULE, MIB_UDP6ROW_OWNER_MODULE>(AF_INET.IP6).Select(tcpConn => new Connection(tcpConn)));
            }
        }

        return ret;
    }

    private unsafe static IEnumerable<TRow> GetAllTCPConnections<TTable, TRow>(uint ipv) where TRow : IConnectionOwnerInfo
    {
        IntPtr ptr = IntPtr.Zero;
        try
        {
            uint buffSize = 0;
            _ = NativeMethods.GetExtendedTcpTable(null, ref buffSize, false, ipv, TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);

            ptr = Marshal.AllocHGlobal((int)buffSize);

            var ret = NativeMethods.GetExtendedTcpTable((void*)ptr, ref buffSize, false, ipv, TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);

            if (ret == 0)
            {
                return ReadConnectionTable<TTable, TRow>(ptr);
            }
            else
            {
                throw new Win32Exception((int)ret);
            }
        }
        finally
        {
            if (ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }

    private unsafe static IEnumerable<TRow> GetAllUDPConnections<TTable, TRow>(uint ipv) where TRow : IConnectionOwnerInfo
    {
        IntPtr ptr = IntPtr.Zero;
        try
        {
            uint buffSize = 0;
            _ = NativeMethods.GetExtendedUdpTable(null, ref buffSize, false, ipv, UDP_TABLE_CLASS.UDP_TABLE_OWNER_MODULE, 0);

            ptr = Marshal.AllocHGlobal((int)buffSize);

            var ret = NativeMethods.GetExtendedUdpTable((void*)ptr, ref buffSize, false, ipv, UDP_TABLE_CLASS.UDP_TABLE_OWNER_MODULE, 0);

            if (ret == 0)
            {
                return ReadConnectionTable<TTable, TRow>(ptr);
            }
            else
            {
                throw new Win32Exception((int)ret);
            }
        }
        finally
        {
            if (ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }

    private static unsafe IEnumerable<TRow> ReadConnectionTable<TTable, TRow>(IntPtr ptr) where TRow : IConnectionOwnerInfo
    {
        var dwNumEntries = Marshal.ReadInt32(ptr);
        // Padding may exist after dwNumEntries member; so we have to compute the actual offset.
        // See https://learn.microsoft.com/en-us/windows/win32/api/tcpmib/ns-tcpmib-mib_tcptable_owner_module#remarks
        var rowPtr = ptr + Marshal.OffsetOf<TTable>(nameof(MIB_TCPTABLE_OWNER_MODULE.table));

        return new Span<TRow>((void*)rowPtr, dwNumEntries).ToArray();
    }

    private static IPAddress? _currentIP;
    public static IPAddress? CurrentIP
    {
        get
        {
            if (_currentIP is null)
            {
                SetCurrentIPAsync();
            }
            return _currentIP;
        }
        set
        {
            if (value != _currentIP)
            {
                _currentIP = value;
                StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(CurrentIP)));
            }
        }
    }

    private static async void SetCurrentIPAsync()
    {
        CurrentIP = await GetPublicIPAddressAsync();
    }

    [GeneratedRegex("^Current IP Address: (?<ip>.*)$", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
    private static partial Regex CurrentIPAddressRegEx();

    public static async Task<IPAddress> GetPublicIPAddressAsync()
    {
        try
        {
            // It's usually not a good idea to instantiate the HttpClient this way (one have to either use a factory or a static client),
            // but this method will be only called once.
            using var client = new HttpClient();

            var response = await client.GetStringAsync("http://checkip.eurodyndns.org/");

            var adr = CurrentIPAddressRegEx().Match(response);

            return IPAddress.Parse(adr.Groups["ip"].ValueSpan.Trim());
        }
        catch
        {
            return IPAddress.None;
        }
    }

    private const int buffer_size = 32;
    private const int max_hops = 30;
    private const int ping_timeout = 4000;
    public static async Task<IList<IPAddress>> GetFullRouteAsync(string adr)
    {
        var ret = new List<IPAddress>();

        using var pong = new Ping();

        var po = new PingOptions(1, true);
        PingReply? r = null;
        var buffer = new byte[buffer_size];
        Array.Fill(buffer, (byte)0);

        for (var i = 1; i < max_hops; i++)
        {
            if (r is not null && r.Status != IPStatus.TimedOut)
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

        ret.Add(IPAddress.Parse(adr));
        return ret;
    }
}
