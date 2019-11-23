using System;
using System.Xml;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

/// <summary>
/// NetshHelper executes netsh commands and parses the resulting xml content.
/// Author: harrwiss / Nov 2019
/// </summary>
namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public enum FiltersContextEnum
    {
        NONE,
        FILTERS,
        WFPSTATE
    }

    public class FilterResult
    {
        public int FilterId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public FiltersContextEnum FoundIn { get; set; } = FiltersContextEnum.NONE;
        public Boolean HasErrors { get; set; } = false;
    }

    /// <summary>
    /// Helper for executing netsh commands and parsing the results.
    /// </summary>
    public static class NetshHelper
    {
        internal static XmlDocument FILTERS_XMLDOC = null;
        internal static XmlDocument WFPSTATE_XMLDOC = null;

        internal static FilterResult FILTER_RESULT_ERROR = new FilterResult
        {
            FilterId = 0,
            Name = "No filter found",
            Description = "",
            HasErrors = true
        };

        internal static void Init(bool refreshData = false)
        {
            if (FILTERS_XMLDOC == null || WFPSTATE_XMLDOC == null || refreshData) { 
                FILTERS_XMLDOC = LoadWfpFilters(); // firewall filters
                WFPSTATE_XMLDOC = LoadWfpState(); // all filters added by other provider e.g. firewall apps
            }
        }

        public static FilterResult FindMatchingFilterInfo(int filterId, bool refreshData = false)
        {
            Init(refreshData);
            if (FILTERS_XMLDOC != null)
            {
                FilterResult fr = FindFilterId(filterId);
                if (fr == null && WFPSTATE_XMLDOC != null)
                {
                   fr = FindWfpStateFilterId(filterId);
                }
                return fr ?? FILTER_RESULT_ERROR;
            }
            return FILTER_RESULT_ERROR;
        }

        public static FilterResult FindMatchingFilterByKey(string filterKey, bool refreshData = false)
        {
            Init(refreshData);
            if (FILTERS_XMLDOC != null)
            {
                FilterResult fr = FindFilterKey(filterKey);
                if (fr == null && WFPSTATE_XMLDOC != null)
                {
                    fr = FindWfpStateFilterKey(filterKey);
                }
                return fr ?? FILTER_RESULT_ERROR;
            }
            return FILTER_RESULT_ERROR;
        }

        internal static XmlDocument LoadWfpFilters()
        {
            XmlDocument xmlDoc;
            String sys32Folder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            RunResult rr = RunCommandCapturing(sys32Folder + @"\netsh.exe", @"wfp show filters file=-");
            if (rr.exitCode == 0)
            {
                xmlDoc = SafeLoadXml(rr.outputData.ToString());
            }
            else
            {
                xmlDoc = null;
                LogHelper.Debug($"netsh error: exitCode={rr.exitCode}\noutput: {rr.outputData.ToString().Substring(1, Math.Min(rr.outputData.Length - 1, 300))}...\nerror: { rr.errorData?.ToString() }");
            }
            return xmlDoc;
        }
        internal static XmlDocument LoadWfpState()
        {
            XmlDocument xmlDoc;
            String sys32Folder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            RunResult rr = RunCommandCapturing(sys32Folder + @"\netsh.exe", @"wfp show state file=-");
            if (rr.exitCode == 0)
            {
                xmlDoc = SafeLoadXml(rr.outputData.ToString());
            }
            else
            {
                xmlDoc = null;
                LogHelper.Debug($"netsh error: exitCode={rr.exitCode}\noutput: {rr.outputData.ToString().Substring(1, Math.Min(rr.outputData.Length - 1, 300))}...\nerror: { rr.errorData?.ToString() }");
            }
            return xmlDoc;
        }

        internal static FilterResult FindFilterId(int filterId)
        {
            XmlNode root = FILTERS_XMLDOC.DocumentElement;
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(FILTERS_XMLDOC.NameTable);
            try
            {
                XmlNode filtersNode = root.FirstChild;
                XmlNode filter = filtersNode.SelectSingleNode("item[filterId=" + filterId + "]", nsmgr);
                FilterResult fr = null;
                if (filter != null)
                {
                    fr = new FilterResult();
                    XmlNode displayData = filter["displayData"];
                    fr.FilterId = filterId;
                    fr.Name = displayData["name"].InnerText;
                    fr.Description = displayData["description"].InnerText;
                    fr.FoundIn = FiltersContextEnum.FILTERS;
                }
                return fr;
            }
            catch (Exception e)
            {
                LogHelper.Error(e.Message, e);
            }
            return null;
        }

        internal static FilterResult FindFilterKey(string filterKey)
        {
            XmlNode root = FILTERS_XMLDOC.DocumentElement;
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(FILTERS_XMLDOC.NameTable);

            try
            {
                XmlNode filtersNode = root.FirstChild;
                XmlNode filter = filtersNode.SelectSingleNode("item[filterKey='" + filterKey + "']", nsmgr);
                FilterResult fr = null;
                if (filter != null)
                {
                    fr = new FilterResult();
                    fr.FilterId = int.Parse(filter["filterId"].InnerText);
                    XmlNode displayData = filter["displayData"];
                    fr.Name = displayData["name"].InnerText;
                    fr.Description = displayData["description"].InnerText;
                    fr.FoundIn = FiltersContextEnum.FILTERS;
                }
                return fr;
            }
            catch (Exception e)
            {
                LogHelper.Error(e.Message, e);
            }
            return null;
        }
        internal static FilterResult FindWfpStateFilterKey(string filterKey)
        {
            XmlNode root = WFPSTATE_XMLDOC.DocumentElement;
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(WFPSTATE_XMLDOC.NameTable);
            try
            {
                // / wfpstate / layers / item[14] / filters / item[1] / filterKey
                XmlNode filtersNode = root.SelectSingleNode("layers");
                XmlNode filter = filtersNode.SelectSingleNode("//filters/item[filterKey='" + filterKey + "']", nsmgr);
                FilterResult fr = null;
                if (filter != null)
                {
                    fr = new FilterResult();
                    fr.FilterId = int.Parse(filter["filterId"].InnerText);
                    XmlNode displayData = filter["displayData"];
                    fr.Name = displayData["name"].InnerText;
                    fr.Description = displayData["description"].InnerText;
                    fr.FoundIn = FiltersContextEnum.WFPSTATE;
                }
                return fr;

            }
            catch (Exception e)
            {
                LogHelper.Error(e.Message, e);
            }
            return null;
        }

        internal static FilterResult FindWfpStateFilterId(int filterId)
        {
            XmlNode root = WFPSTATE_XMLDOC.DocumentElement;
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(WFPSTATE_XMLDOC.NameTable);
            try
            {
                XmlNode filtersNode = root.SelectSingleNode("layers");
                XmlNode filter = filtersNode.SelectSingleNode("//filters/item[filterId=" + filterId + "]", nsmgr);
                FilterResult fr = null;
                if (filter != null)
                {
                    fr = new FilterResult();
                    XmlNode displayData = filter["displayData"];
                    fr.Name = displayData["name"].InnerText;
                    fr.Description = displayData["description"].InnerText;
                    fr.FoundIn = FiltersContextEnum.WFPSTATE;
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

        static RunResult RunCommandCapturing(string command, string args, string workingDir = null)
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
                    // note: outputData.appendLine should not be used because it can cause a break in the middle of an output line when the buffer is reached
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
        internal static XmlDocument SafeLoadXml(string xml)
        {
            XmlReader reader = null;
            XmlDocument xmlDoc = null;
            try
            {
                // CA3075 - Unclear message: Unsafe overload of 'LoadXml' 
                // see: https://github.com/dotnet/roslyn-analyzers/issues/2477
                xmlDoc = new XmlDocument() { XmlResolver = null };
                StringReader sreader = new System.IO.StringReader(xml);
                reader = XmlReader.Create(sreader, new XmlReaderSettings() { XmlResolver = null });
                xmlDoc.Load(reader);
            }
            catch (Exception xe)
            {
                xmlDoc = null;
                LogHelper.Error(xe.Message, xe);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Dispose();
                }
            }
            return xmlDoc;
        }

        internal static XmlDocument UnSafeLoadXml(string xml)
        {
            XmlDocument xmlDoc = null;
            try
            {
                xmlDoc = new XmlDocument
                {
                    InnerXml = xml,
                    XmlResolver = null
                };

                //File.WriteAllText(@"c:\temp\filters_compare.xml", xml);
            }
            catch (Exception xe)
            {
                xmlDoc = null;
                LogHelper.Error(xe.Message, xe);
            }
            return xmlDoc;
        }
    }
}
