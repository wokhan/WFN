using System;
using System.Windows.Media;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels
{
    public class LogEntryViewModel
    {
        public string Pid { get; set; }
        public DateTime Timestamp { get; set; }
        public ImageSource Icon { get; set; }
        public string FileName { get; set; }
        public string Path { get; set; }
        public string FriendlyPath { get; set; }
        public string TargetIP { get; set; }
        public string TargetPort { get; set; }
        public string Protocol { get; set; }
        public string Direction { get; set; }
        public string FilterId { get; set; }

        public string Reason { get; set; }
        public string Reason_Info { get; set; }

    }

}
