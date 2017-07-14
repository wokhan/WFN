using System;
using System.Windows.Media;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels
{
    public class LogEntryViewModel
    {
        public DateTime CreationTime { get; set; }
        public ImageSource Icon { get; set; }
        public string FriendlyPath { get; set; }
        public string TargetIP { get; set; }
        public string TargetPort { get; set; }
        public string Protocol { get; set; }
    }

}
