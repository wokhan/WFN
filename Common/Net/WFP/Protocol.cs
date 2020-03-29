using NetFwTypeLib;
using System;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Common.Net.WFP
{
    public class Protocol
    {
        public const int TCP = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
        public const int UDP = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP;
        public const int ANY = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY;

        public static bool IsIPProtocol(int protocol)
        {
            //Used to check whether this protocol supports ports.
            return protocol == TCP || protocol == UDP;
        }

        public static string GetProtocolAsString(int protocol)
        {
            //These are the IANA protocol numbers.
            // Source: https://www.iana.org/assignments/protocol-numbers/protocol-numbers.xhtml
            // TODO: Add all the others and use an array?
            switch (protocol)
            {
                case 1:
                    return "ICMP";

                case 2:
                    return "IGMP"; //Used by OpenVPN, for example.

                case Protocol.TCP: //6
                    return "TCP";

                case Protocol.UDP: //17
                    return "UDP";

                case 40:
                    return "IL"; // IL Transport protocol

                case 42:
                    return "SDRP"; // Source Demand Routing Protocol

                case 47:
                    return "GRE"; //Used by PPTP, for example.

                case 58:
                    return "ICMPv6";

                case 136:
                    return "UDPLite";

                case 36:
                    return "XTP";

                default:
                    LogHelper.Warning("Unknown protocol type: " + protocol.ToString());
                    return protocol >= 0 ? protocol.ToString() : "Unknown";
            }
        }

        public static bool IsUnknown(int protocol)
        {
            return protocol < 0;
        }
    }
}