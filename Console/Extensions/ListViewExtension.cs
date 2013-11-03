using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace WindowsFirewallNotifierConsole.Extensions
{

    static class ListViewExtension
    {
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, int lParam);

        public static void SetDoubleBuffered(this ListView lv)
        {
            int styles = (int)SendMessage(lv.Handle, (int)0x1000 + 55, IntPtr.Zero, 0);
            styles |= 0x00010000 | 0x00008000;
            SendMessage(lv.Handle, (int)0x1000 + 54, IntPtr.Zero, styles);
        }
    }
}
