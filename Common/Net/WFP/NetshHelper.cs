using System;
using System.Xml;
using System.Diagnostics;
using System.Text;
using System.IO;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

/// <summary>
/// NetshHelper executes netsh commands and parses the resulting xml content.
/// Author: harrwiss / Nov 2019
/// </summary>
namespace Wokhan.WindowsFirewallNotifier.Common.Net.WFP
{

    /// <summary>
    /// Helper for executing netsh commands and parsing the results.
    /// </summary>
    public static class NetshHelper
    {
        internal static XmlDocument? FiltersXmlDoc = null;
        internal static XmlDocument? WFPStateXmlDoc = null;

        internal static FilterResult FILTER_RESULT_ERROR = new FilterResult
        {
            FilterId = 0,
            Name = "No filter found",
            Description = "",
            HasErrors = true
        };

        internal static void Init(bool refreshData = false)
        {
            if (FiltersXmlDoc == null || WFPStateXmlDoc == null || refreshData)
            {
                FiltersXmlDoc = LoadWfpFilters(); // firewall filters
                WFPStateXmlDoc = LoadWfpState(); // all filters added by other provider e.g. firewall apps
            }
        }

        public static FilterResult FindMatchingFilterInfo(int filterId, bool refreshData = false)
        {
            Init(refreshData);
            if (FiltersXmlDoc != null)
            {
                FilterResult? fr = FindFilterId(filterId);
                if (fr == null && WFPStateXmlDoc != null)
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
            if (FiltersXmlDoc != null)
            {
                FilterResult fr = FindFilterKey(filterKey);
                if (fr == null && WFPStateXmlDoc != null)
                {
                    fr = FindWfpStateFilterKey(filterKey);
                }
                return fr ?? FILTER_RESULT_ERROR;
            }
            return FILTER_RESULT_ERROR;
        }

        internal static XmlDocument? LoadWfpFilters()
        {
            XmlDocument? xmlDoc = null;
            var sys32Folder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            RunResult rr = RunCommandCapturing(sys32Folder + @"\netsh.exe", @"wfp show filters file=-");
            if (rr.exitCode == 0)
            {
                xmlDoc = SafeLoadXml(rr.outputData.ToString());
            }
            else
            {
                LogHelper.Debug($"netsh error: exitCode={rr.exitCode}\noutput: {rr.outputData.ToString().Substring(1, Math.Min(rr.outputData.Length - 1, 300))}...\nerror: { rr.errorData?.ToString() }");
            }
            return xmlDoc;
        }

        internal static XmlDocument? LoadWfpState()
        {
            XmlDocument? xmlDoc;
            var sys32Folder = Environment.GetFolderPath(Environment.SpecialFolder.System);
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

        internal static FilterResult? FindFilterId(int filterId)
        {
            XmlNode root = FiltersXmlDoc.DocumentElement;
            var nsmgr = new XmlNamespaceManager(FiltersXmlDoc.NameTable);
            try
            {
                XmlNode filtersNode = root.FirstChild;
                XmlNode filter = filtersNode.SelectSingleNode("item[filterId=" + filterId + "]", nsmgr);
                FilterResult? fr = null;
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

        internal static FilterResult? FindFilterKey(string filterKey)
        {
            XmlNode root = FiltersXmlDoc.DocumentElement;
            var nsmgr = new XmlNamespaceManager(FiltersXmlDoc.NameTable);

            try
            {
                XmlNode filtersNode = root.FirstChild;
                XmlNode filter = filtersNode.SelectSingleNode("item[filterKey='" + filterKey + "']", nsmgr);
                FilterResult? fr = null;
                if (filter != null)
                {
                    XmlNode displayData = filter["displayData"];
                    fr = new FilterResult
                    {
                        FilterId = int.Parse(filter["filterId"].InnerText),
                        Name = displayData["name"].InnerText,
                        Description = displayData["description"].InnerText,
                        FoundIn = FiltersContextEnum.FILTERS
                    };
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
            XmlNode root = WFPStateXmlDoc.DocumentElement;
            var nsmgr = new XmlNamespaceManager(WFPStateXmlDoc.NameTable);
            try
            {
                // / wfpstate / layers / item[14] / filters / item[1] / filterKey
                XmlNode filtersNode = root.SelectSingleNode("layers");
                XmlNode filter = filtersNode.SelectSingleNode("//filters/item[filterKey='" + filterKey + "']", nsmgr);
                FilterResult? fr = null;
                if (filter != null)
                {
                    XmlNode displayData = filter["displayData"];
                    fr = new FilterResult
                    {
                        FilterId = int.Parse(filter["filterId"].InnerText),
                        Name = displayData["name"].InnerText,
                        Description = displayData["description"].InnerText,
                        FoundIn = FiltersContextEnum.WFPSTATE
                    };
                }
                return fr;

            }
            catch (Exception e)
            {
                LogHelper.Error(e.Message, e);
            }
            return null;
        }

        internal static FilterResult? FindWfpStateFilterId(int filterId)
        {
            XmlNode root = WFPStateXmlDoc.DocumentElement;
            var nsmgr = new XmlNamespaceManager(WFPStateXmlDoc.NameTable);
            try
            {
                XmlNode filtersNode = root.SelectSingleNode("layers");
                XmlNode filter = filtersNode.SelectSingleNode("//filters/item[filterId=" + filterId + "]", nsmgr);
                FilterResult? fr = null;
                if (filter != null)
                {
                    XmlNode displayData = filter["displayData"];
                    fr = new FilterResult
                    {
                        Name = displayData["name"].InnerText,
                        Description = displayData["description"].InnerText,
                        FoundIn = FiltersContextEnum.WFPSTATE
                    };
                }
                return fr;
            }
            catch (Exception e)
            {
                LogHelper.Error(e.Message, e);
            }
            return null;
        }

        private class RunResult
        {
            internal int dataLineCnt = 0;
            internal StringBuilder errorData = new StringBuilder();
            internal StringBuilder outputData = new StringBuilder();
            internal int exitCode = -1;
        }

        static RunResult RunCommandCapturing(string command, string args, string? workingDir = null)
        {
            var rr = new RunResult();
            try
            {
                using (var p = new Process())
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
                    p.ErrorDataReceived += (sender, arg) => rr.errorData.AppendLine(arg.Data);
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
        internal static XmlDocument? SafeLoadXml(string xml)
        {
            XmlDocument? xmlDoc = null;
            try
            {
                using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings() { XmlResolver = null });

                // CA3075 - Unclear message: Unsafe overload of 'LoadXml' 
                // see: https://github.com/dotnet/roslyn-analyzers/issues/2477
                xmlDoc = new XmlDocument() { XmlResolver = null };
                xmlDoc.Load(reader);
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
