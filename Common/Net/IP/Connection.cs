using System;
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using Windows.Win32.NetworkManagement.IpHelper;

using Wokhan.WindowsFirewallNotifier.Common.Logging;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP;

public record Connection
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

    private MIB_TCP6ROW? tcp6MIBRow;
    
    public bool IsMonitored { get; private set; }

    
    private IConnectionOwnerInfo sourceRow;

    internal Connection(MIB_TCPROW_OWNER_MODULE tcpRow)
    {
        sourceRow = tcpRow;

        OwningPid = tcpRow.dwOwningPid;
        LocalAddress = new IPAddress(tcpRow.dwLocalAddr);
        LocalPort = IPHelper.GetRealPort(tcpRow.dwLocalPort);
        if (!tcpRow.dwState.Equals(MIB_TCP_STATE.MIB_TCP_STATE_LISTEN))
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
        if (!tcp6Row.dwState.Equals(MIB_TCP_STATE.MIB_TCP_STATE_LISTEN))
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

    public bool TryEnableStats()
    {
        if (Protocol != "TCP" || State == ConnectionStatus.LISTENING || IPAddress.IsLoopback(RemoteAddress))
        {
            return false;
        }

        var setting = new TCP_ESTATS_BANDWIDTH_RW_v0() { EnableCollectionInbound = TCP_BOOLEAN_OPTIONAL.TcpBoolOptEnabled, EnableCollectionOutbound = TCP_BOOLEAN_OPTIONAL.TcpBoolOptEnabled };
        // Note: passing tcp6MIBROW as a parameter even for TCP V4 connections, but it will not be used as tcpMIBRow_LH (for V4) is taken directly from sourcerow
        var r = sourceRow.SetPerTcpConnectionEStats(ref setting, tcp6MIBRow);
        
        IsMonitored = (r == NO_ERROR && setting.EnableCollectionInbound == TCP_BOOLEAN_OPTIONAL.TcpBoolOptEnabled && setting.EnableCollectionOutbound == TCP_BOOLEAN_OPTIONAL.TcpBoolOptEnabled);

        return IsMonitored;
    }

    bool _firstPassDone;
    ulong _lastInboundReadValue;
    ulong _lastOutboundReadValue;

    public (ulong InboundBandwidth, ulong OutboundBandwidth, bool IsMonitored) GetEstimatedBandwidth()
    {
        if (!IsMonitored)
        {
            return (0, 0, false);
        }

        try
        {
            // Note: passing tcp6MIBROW as a parameter even for TCP V4 connections, but it will not be used as tcpMIBRow_LH (for V4) is taken directly from sourcerow
            var rodObjectNullable = sourceRow.GetPerTcpConnectionEState(tcp6MIBRow);

            if (rodObjectNullable is null)
            {
                IsMonitored = false;
                return (0, 0, false);
            }

            var rodObject = rodObjectNullable.Value;

            // Fix according to https://docs.microsoft.com/en-us/windows/win32/api/iphlpapi/nf-iphlpapi-setpertcpconnectionestats
            // One must subtract the previously read value to get the right one (as reenabling statistics doesn't work as before starting from Win 10 1709)
            ulong inbound = 0;
            ulong outbound = 0;

            // Ignore first pass as data will be wrong (as observed during testing)
            if (_firstPassDone)
            {
                inbound = rodObject.InboundBandwidth >= _lastInboundReadValue ? rodObject.InboundBandwidth - _lastInboundReadValue : rodObject.InboundBandwidth;
                outbound = rodObject.OutboundBandwidth >= _lastOutboundReadValue ? rodObject.OutboundBandwidth - _lastOutboundReadValue : rodObject.OutboundBandwidth;
            }

            _firstPassDone = true;

            _lastInboundReadValue = rodObject.InboundBandwidth;
            _lastOutboundReadValue = rodObject.OutboundBandwidth;

            return (inbound, outbound, true);
        }
        catch (InvalidOperationException)
        {
            IsMonitored = false;

            _lastInboundReadValue = 0;
            _lastOutboundReadValue = 0;

            return (0, 0, false);
        }
        catch (Win32Exception we) when (we.NativeErrorCode == IPHelper.ERROR_NOT_FOUND)
        {
            IsMonitored = false;

            _lastInboundReadValue = 0;
            _lastOutboundReadValue = 0;

            return (0, 0, false);
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

    public void UpdateWith(Connection rawConnection)
    {
        this.State = rawConnection.State;
        this.RemoteAddress = rawConnection.RemoteAddress;
        this.RemotePort = rawConnection.RemotePort;
        this.IsLoopback = rawConnection.IsLoopback;
        
        this.tcp6MIBRow = rawConnection.tcp6MIBRow;
    }
}
