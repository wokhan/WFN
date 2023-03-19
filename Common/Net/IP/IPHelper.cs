using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Wokhan.WindowsFirewallNotifier.Common.IO.Streams;
using Wokhan.WindowsFirewallNotifier.Common.Logging;
using Wokhan.WindowsFirewallNotifier.Common.Net.IP.TCP;
using Wokhan.WindowsFirewallNotifier.Common.Net.IP.UDP;


namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP;

public abstract partial class IPHelper
{
    private const string MAX_USER_PORT_REGISTRY_KEY = "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters";
    private const string MAX_USER_PORT_REGISTRY_VALUE = "MaxUserPort";

    protected const uint NO_ERROR = 0;
    protected const uint ERROR_INSUFFICIENT_BUFFER = 122;
    protected const uint ERROR_NOT_FOUND = 1168;

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

    public static int GetMaxUserPort()
    {
        using var maxUserPortKey = Registry.LocalMachine.OpenSubKey(MAX_USER_PORT_REGISTRY_KEY, false);
        var maxUserPortValue = maxUserPortKey?.GetValue(MAX_USER_PORT_REGISTRY_VALUE);
        if (maxUserPortValue is null)
        {
            //Default from Windows Vista and up
            return 49152;
        }

        return Convert.ToInt32(maxUserPortValue);
    }


    internal delegate uint GetOwnerModuleDelegate<T>(T pTcpEntry, TCPIP_OWNER_MODULE_INFO_CLASS Class, IntPtr Buffer, ref uint pdwSize);

    internal static Owner? GetOwningModuleInternal<TRow>(GetOwnerModuleDelegate<TRow> getOwnerModule, TRow row) where TRow : IConnectionOwnerInfo
    {
        Owner? ret = null;
        /*if (ownerCache.TryGetValue(row, out ret))
        {
            return ret;
        }*/

        if (row.OwningPid == 0)
        {
            return Owner.System;
        }

        IntPtr buffer = IntPtr.Zero;
        try
        {
            uint buffSize = 0;
            var retn = getOwnerModule(row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, IntPtr.Zero, ref buffSize);
            if (retn != NO_ERROR && retn != ERROR_INSUFFICIENT_BUFFER)
            {
                //Cannot get owning module for this connection
                LogHelper.Info("Unable to get the connection owner: ownerPid=" + row.OwningPid + " remoteAdr=" + row.RemoteAddress + ":" + row.RemotePort);
                return ret;
            }
            if (buffSize == 0)
            {
                //No buffer? Probably means we can't retrieve any information about this connection; skip it
                LogHelper.Info("Unable to get the connection owner (no buffer).");
                return ret;
            }
            buffer = Marshal.AllocHGlobal((int)buffSize);

            //GetOwnerModuleFromTcpEntry needs the fields of TCPIP_OWNER_MODULE_INFO_BASIC to be NULL
            NativeMethods.RtlZeroMemory(buffer, buffSize);

            var resp = getOwnerModule(row, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, buffer, ref buffSize);
            if (resp == 0)
            {
                ret = new Owner(Marshal.PtrToStructure<TCPIP_OWNER_MODULE_BASIC_INFO>(buffer));
            }
            else if (resp != ERROR_NOT_FOUND) // Ignore closed connections
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
    /// <summary>
    /// Returns details about connection of localPort by process identified by pid.
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    public static Owner? GetOwner(uint pid, int localPort)
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
            ret = ret.Concat(TCPHelper.GetAllTCP6Connections());
            if (!tcpOnly)
            {
                ret = ret.Concat(UDPHelper.GetAllUDP6Connections());
            }
        }

        return ret;
    }


    private static IPAddress? _currentIP;
    public static IPAddress CurrentIP
    {
        get => _currentIP ??= GetPublicIpAddress();
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
            var adr = CurrentIPAddressRegEx().Match(ans);

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

    [GeneratedRegex("Current IP Address: (.*)", RegexOptions.Singleline)]
    private static partial Regex CurrentIPAddressRegEx();
}
