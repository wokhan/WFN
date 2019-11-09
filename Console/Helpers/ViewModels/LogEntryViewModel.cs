using System;
using System.Windows.Media;
using System.Net;
using Harrwiss.Common.Network.Helper;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Common;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels
{
    public class LogEntryViewModel
    {
        public int Pid { get; set; }
        public DateTime Timestamp { get; set; }
        public ImageSource Icon { get; set; }
        public string FileName { get; set; }
        public string Path { get; set; }
        public string FriendlyPath { get; set; }
        public string ServiceName { get; set; }
        public string TargetIP { get; set; }
        public string TargetHostName { get
            {
                try
                {
                    if (Settings.Default.EnableDnsResolver && DnsResolver.CachedIPHostEntryDict.TryGetValue(IPAddress.Parse(TargetIP), out CachedIPHostEntry value))
                    {
                        return value.DisplayText;
                    }
                    else
                    {
                        return "...";
                    }
                }
                catch (Exception e)
                {
                    LogHelper.Warning(e.Message);
                }
                return "";
            }
        }
        public string TargetPort { get; set; }
        public string Protocol { get; set; }
        public string Direction { get; set; }
        public string FilterId { get; set; }

        public string Reason { get; set; }
        public string Reason_Info { get; set; }

        public SolidColorBrush ReasonColor { get; set; }

        public SolidColorBrush DirectionColor { get; set; }

    }

}
