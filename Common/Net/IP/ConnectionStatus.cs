namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP
{
    // MUST match MIB_TCP_STATE enumeration
    public enum ConnectionStatus
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
}
