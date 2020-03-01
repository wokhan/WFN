using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wokhan.WindowsFirewallNotifier.Notifier.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Notifier.UI.Events
{
    public class ActivityEventArgs : EventArgs
    {
        public ActivityEnum Activity { get; set; }

        public CurrentConn CurrentConnection { get; set; }

        public DateTime TimeStamp { get; } = DateTime.Now;

        public enum ActivityEnum
        {
            Allow, Block, AllowTemp, BlockTemp
        }
        public ActivityEventArgs(ActivityEnum activity, CurrentConn currentConnection)
        {
            Activity = activity;
            CurrentConnection = currentConnection;
        }
    }
}
