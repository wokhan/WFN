using System;
using System.Windows.Media;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels
{
    public class LogEntryViewModel
    {
        public DateTime CreationTime { get; set; }
        public ImageSource Icon { get; set; }
        public string FriendlyPath { get; set; }
        public string Replacement5 { get; set; }
        public string Protocol { get; set; }
        public string Replacement6 { get; set; }
    }

}
