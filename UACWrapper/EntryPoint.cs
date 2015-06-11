using System;
using System.Diagnostics;

namespace Wokhan.WindowsFirewallNotifier.UACWrapper
{
    class EntryPoint
    {

        /// <summary>
        /// Wrapper app to launch the Console with inherited elevated rights (much easier than dealing with rights in the Console itself).
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Process.Start("WFN.exe", "iselevated");
        }
    }
}


