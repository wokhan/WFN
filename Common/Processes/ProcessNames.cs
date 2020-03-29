using System;
using System.Collections.Generic;
using System.Text;

namespace Wokhan.WindowsFirewallNotifier.Common.Processes
{

    /// <summary>
    /// Common process names and associated attributes simulating an enum.
    /// </summary>
    public class ProcessNames
    {
        public static readonly ProcessNames WFN = new ProcessNames("WFN", "WFN.exe");
        public static readonly ProcessNames Notifier = new ProcessNames("Notifier", "Notifier.exe");

        private ProcessNames(string processName, string exeName)
        {
            ProcessName = processName;
            FileName = exeName;
        }
        
        public string ProcessName { get; set; }
        public string FileName { get; set; }

        public override string ToString()
        {
            return ProcessName;
        }
    }
}
