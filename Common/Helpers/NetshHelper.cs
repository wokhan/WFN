using System;
using System.Xml;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    /// <summary>
    /// Helper for executing netsh commands and parsing the results.
    /// </summary>
    public class NetshHelper
    {
        public class FilterResult
        {
            public string name { get; set; }
            public string description { get; set; }
        }

        internal static XmlDocument xmlDoc = null;
        public static FilterResult getBlockingFilter(int filterId, bool refreshData = false)
        {
            if (xmlDoc == null || refreshData)
            {
                String sys32Folder = Environment.GetFolderPath(Environment.SpecialFolder.System);
                RunResult rr = runCommandCapturing(sys32Folder + @"\netsh.exe", @"wfp show filters file=-");
                if (rr.exitCode == 0)
                {
                    xmlDoc = new XmlDocument
                    {
                        InnerXml = rr.outputData.ToString()
                    };
                }
                else
                {
                    xmlDoc = null;
                    LogHelper.Debug($"netsh error: exitCode={rr.exitCode}\noutput: {rr.outputData.ToString().Substring(1, 300)}...\nerror: { rr.errorData.ToString() }");
                }
            }

            if (xmlDoc != null)
            {
                FilterResult fr = parseFilters(filterId, xmlDoc);
                return fr;
            }
            return null;
        }

        static FilterResult parseFilters(int filterId, XmlDocument doc)
        {
            XmlNode root = doc.DocumentElement;

            // Add the namespace.  
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);

            try
            {
                XmlNode filtersNode = root.FirstChild;
                XmlNode filter = filtersNode.SelectSingleNode("item[filterId=" + filterId + "]", nsmgr);
                FilterResult fr = null;
                if (filter != null)
                {
                    fr = new FilterResult();
                    XmlNode displayData = filter["displayData"];
                    fr.name = displayData["name"].InnerText;
                    fr.description = displayData["description"].InnerText;
                }
                return fr;
            }
            catch (Exception e)
            {
                LogHelper.Error(e.Message, e);
            }
            return null;
        }

        internal class RunResult
        {
            internal int dataLineCnt = 0;
            internal StringBuilder errorData = new StringBuilder();
            internal StringBuilder outputData = new StringBuilder();
            internal int exitCode = -1;
        }

        static RunResult runCommandCapturing(string command, string args, string workingDir = null)
        {
            RunResult rr = new RunResult();
            try
            {
                using (Process p = new Process())
                {
                    // set start info
                    p.StartInfo = new ProcessStartInfo(command, args)
                    {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        WorkingDirectory = string.IsNullOrWhiteSpace(workingDir) ? Path.GetTempPath() : workingDir,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    // note: outputData.appendLine should not be used because it can cause a break in the middle of a output line when the buffer is reached
                    p.OutputDataReceived += (sender, arg) => { rr.outputData.Append(arg.Data); rr.dataLineCnt++; };
                    p.ErrorDataReceived += (sender, arg) => { rr.errorData.AppendLine(arg.Data); };
                    p.EnableRaisingEvents = false;
                    //p.Exited += onProcessExit;
                    p.Start();
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    p.WaitForExit(10000);   // wait 10s max

                    rr.exitCode = p.ExitCode;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
                rr.errorData.AppendLine(ex.Message);
            }
            return rr;
        }
    }
}
