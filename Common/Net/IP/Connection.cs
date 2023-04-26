using System;
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.NetworkManagement.IpHelper;

using Wokhan.WindowsFirewallNotifier.Common.Logging;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP;

public class Connection
{
    private const uint NO_ERROR = 0;
    private const uint ERROR_INSUFFICIENT_BUFFER = 122;
    private const uint ERROR_NOT_FOUND = 1168;

    public IPAddress RemoteAddress { get; private set; } = IPAddress.None;

    public int RemotePort { get; private set; } = -1;

    public IPAddress LocalAddress { get; private set; }

    public int LocalPort { get; private set; }

    public Owner? OwnerModule { get; private set; }

    public string Protocol { get; init; } = "TCP";

    public DateTime? CreationTime { get; init; }

    public uint OwningPid { get; init; }

    public ConnectionStatus State { get; private set; } = ConnectionStatus.NOT_APPLICABLE;

    public bool IsLoopback { get; private set; }

    private MIB_TCP6ROW tcp6MIBRow;

    private IConnectionOwnerInfo sourceRow;

    unsafe delegate uint GetOwnerModuleDelegate(object ROW, TCPIP_OWNER_MODULE_INFO_CLASS infoClass, void* buffer, ref int buffSize);

    GetOwnerModuleDelegate getOwnerModule;

    internal Connection(MIB_TCPROW_OWNER_MODULE tcpRow)
    {
        sourceRow = tcpRow;

        OwningPid = tcpRow.dwOwningPid;
        LocalAddress = new IPAddress(tcpRow.dwLocalAddr);
        LocalPort = IPHelper.GetRealPort(tcpRow.dwLocalPort);
        if (tcpRow.dwState != (uint)MIB_TCP_STATE.MIB_TCP_STATE_LISTEN)
        {
            RemoteAddress = new IPAddress(tcpRow.dwRemoteAddr);
            RemotePort = IPHelper.GetRealPort(tcpRow.dwRemotePort);
        }
        OwnerModule = GetOwningModule();
        State = (ConnectionStatus)tcpRow.dwState;
        CreationTime = tcpRow.liCreateTimestamp == 0 ? null : DateTime.FromFileTime(tcpRow.liCreateTimestamp);
        IsLoopback = IPAddress.IsLoopback(RemoteAddress);
    }

    internal Connection(MIB_TCP6ROW_OWNER_MODULE tcp6Row)
    {
        sourceRow = tcp6Row;

        this.tcp6MIBRow = new MIB_TCP6ROW() { State = (MIB_TCP_STATE)tcp6Row.dwState, LocalAddr = new() { u = new() { Byte = tcp6Row.ucLocalAddr } }, RemoteAddr = new() { u = new() { Byte = tcp6Row.ucRemoteAddr } }, dwLocalPort = tcp6Row.dwLocalPort, dwRemotePort = tcp6Row.dwRemotePort };

        OwningPid = tcp6Row.dwOwningPid;
        LocalAddress = new IPAddress(tcp6Row.ucLocalAddr.AsSpan().ToArray());
        LocalPort = IPHelper.GetRealPort(tcp6Row.dwLocalPort);
        if (tcp6Row.dwState != (uint)MIB_TCP_STATE.MIB_TCP_STATE_LISTEN)
        {
            RemoteAddress = new IPAddress(tcp6Row.ucRemoteAddr.AsSpan().ToArray());
            RemotePort = IPHelper.GetRealPort(tcp6Row.dwRemotePort);
        }
        OwnerModule = GetOwningModule();
        State = (ConnectionStatus)tcp6Row.dwState;
        CreationTime = tcp6Row.liCreateTimestamp == 0 ? null : DateTime.FromFileTime(tcp6Row.liCreateTimestamp);
        IsLoopback = IPAddress.IsLoopback(RemoteAddress);
    }

    internal Connection(MIB_UDPROW_OWNER_MODULE udpRow)
    {
        sourceRow = udpRow;

        OwningPid = udpRow.dwOwningPid;
        LocalAddress = new IPAddress(udpRow.dwLocalAddr);
        LocalPort = IPHelper.GetRealPort(udpRow.dwLocalPort);
        OwnerModule = GetOwningModule();
        Protocol = "UDP";
        CreationTime = udpRow.liCreateTimestamp == 0 ? null : DateTime.FromFileTime(udpRow.liCreateTimestamp);
    }

    internal Connection(MIB_UDP6ROW_OWNER_MODULE udp6Row)
    {
        sourceRow = udp6Row;

        OwningPid = udp6Row.dwOwningPid;
        LocalAddress = new IPAddress(udp6Row.ucLocalAddr.AsSpan().ToArray());
        LocalPort = IPHelper.GetRealPort(udp6Row.dwLocalPort);
        OwnerModule = GetOwningModule();
        Protocol = "UDP";
        CreationTime = udp6Row.liCreateTimestamp == 0 ? null : DateTime.FromFileTime(udp6Row.liCreateTimestamp);
    }

    private bool EnsureStats(ref bool isAccessDenied)
    {
        if (Protocol != "TCP")
        {
            throw new InvalidOperationException("Statistics are not available for non-TCP connections. Please check first the connection's protocol.");
        }

        if (isAccessDenied || State != ConnectionStatus.ESTABLISHED || IPAddress.IsLoopback(RemoteAddress))
        {
            return false;
        }

        var result = new TCP_ESTATS_BANDWIDTH_RW_v0() { EnableCollectionInbound = TCP_BOOLEAN_OPTIONAL.TcpBoolOptEnabled, EnableCollectionOutbound = TCP_BOOLEAN_OPTIONAL.TcpBoolOptEnabled };
        var r = sourceRow.SetPerTcpConnectionEStats(ref result, tcp6MIBRow);
        if (r != 0)
        {
            throw new Win32Exception((int)r);
        }

        if (result.EnableCollectionInbound != TCP_BOOLEAN_OPTIONAL.TcpBoolOptEnabled || result.EnableCollectionOutbound != TCP_BOOLEAN_OPTIONAL.TcpBoolOptEnabled)
        {
            isAccessDenied = true;
            return false;
        }

        return true;
    }

    ulong _lastInboundReadValue;
    ulong _lastOutboundReadValue;

    //TODO: not fond of those ref params, but using an interface prevents me to use local private fields - and using a property with a proper setter would result in a backing field creatino, breaking the initial struct layout.
    public (ulong InboundBandwidth, ulong OutboundBandwidth) GetEstimatedBandwidth(ref bool isAccessDenied)
    {
        if (!EnsureStats(ref isAccessDenied))
        {
            _lastInboundReadValue = 0;
            _lastOutboundReadValue = 0;

            return (0, 0);
        }

        try
        {
            var rodObjectNullable = sourceRow.GetPerTcpConnectionEState(tcp6MIBRow);

            if (rodObjectNullable is null)
            {
                isAccessDenied = true;
                return (0, 0);
            }

            var rodObject = rodObjectNullable.Value;

            // Fix according to https://docs.microsoft.com/en-us/windows/win32/api/iphlpapi/nf-iphlpapi-setpertcpconnectionestats
            // One must subtract the previously read value to get the right one (as reenabling statistics doesn't work as before starting from Win 10 1709)
            var inbound = rodObject.InboundBandwidth >= _lastInboundReadValue ? rodObject.InboundBandwidth - _lastInboundReadValue : rodObject.InboundBandwidth;
            var outbound = rodObject.OutboundBandwidth >= _lastOutboundReadValue ? rodObject.OutboundBandwidth - _lastOutboundReadValue : rodObject.OutboundBandwidth;

            _lastInboundReadValue = rodObject.InboundBandwidth;
            _lastOutboundReadValue = rodObject.OutboundBandwidth;

            return (inbound, outbound);
        }
        catch (Win32Exception we) when (we.NativeErrorCode == IPHelper.ERROR_NOT_FOUND)
        {
            _lastInboundReadValue = 0;
            _lastOutboundReadValue = 0;

            return (0, 0);
        }
    }


    internal Owner? GetOwningModule()
    {
        if (OwningPid is 0 or 4)
        {
            return Owner.System;
        }

        Owner? ret = null;

        var buffer = IntPtr.Zero;
        try
        {
            // No need to set the proper size as it will be recomputed. See https://learn.microsoft.com/en-us/windows/win32/api/iphlpapi/nf-iphlpapi-getownermodulefromtcp6entry#remarks
            // Meaning that we cannont get the right size from the structure alone, we need to know the exact size including the resulting strings...
            uint buffSize = 0;

            var retn = sourceRow.GetOwnerModule(IntPtr.Zero, ref buffSize);
            if (retn != NO_ERROR && retn != ERROR_INSUFFICIENT_BUFFER)
            {
                //Cannot get owning module for this connection
                LogHelper.Info("Unable to get the connection owner: ownerPid=" + OwningPid + " remoteAdr=" + RemoteAddress + ":" + RemotePort);
                return ret;
            }

            if (buffSize == 0)
            {
                //No buffer? Probably means we can't retrieve any information about this connection; skip it
                LogHelper.Info("Unable to get the connection owner (no buffer).");
                return ret;
            }

            buffer = Marshal.AllocHGlobal((int)buffSize);

            var resp = sourceRow.GetOwnerModule(buffer, ref buffSize);
            if (resp == NO_ERROR)
            {
                var ownerInfo = Marshal.PtrToStructure<TCPIP_OWNER_MODULE_BASIC_INFO>(buffer);
                ret = new Owner(ownerInfo.pModuleName.ToString(), ownerInfo.pModulePath.ToString());
            }
            else if (resp != ERROR_NOT_FOUND) // Ignore closed connections
            {
                LogHelper.Error("Unable to get the connection owner.", new Win32Exception((int)resp));
            }

            return ret;
        }
        catch (Exception e)
        {
            return new Owner("ERROR", "");
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
