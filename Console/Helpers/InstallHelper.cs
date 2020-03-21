using System;
using System.IO;
using System.Security.Principal;
using System.Text;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Common.Properties;
using System.Reflection;
using System.ServiceProcess;
using System.Linq;
using Wokhan.WindowsFirewallNotifier.Common;
using static Wokhan.WindowsFirewallNotifier.Common.Helpers.FirewallHelper.CustomRule;
using System.Diagnostics.CodeAnalysis;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers
{
    public class InstallHelper
    {
        /// <summary>
        /// 
        /// </summary>
        public static bool UninstallCheck(bool disableAuditPolicy, bool removeNotifierTask, Func<Func<bool>, string, string, bool> funcDelegate)
        {
            if (funcDelegate is null)
            {
                throw new ArgumentNullException(nameof(funcDelegate));
            }
            LogHelper.Debug("UninstallCheck");

            if (disableAuditPolicy)
            {
                if (!funcDelegate(
                    () => SetAuditPolConnection(enableSuccess: false, enableFailure: true)
                    , "Reset audit policy to failure only."
                    , Resources.MSG_UNINST_DISABLE_LOG_ERR)) return false;
            }

            if (removeNotifierTask)
            {
                if (!funcDelegate(() => RemoveTask()
                    , Resources.MSG_UNINST_OK
                , Resources.MSG_UNINST_TASK_ERR)) return false;
            }

            Settings.Default.IsInstalled = false;
            Settings.Default.Save();

            return true;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="pass"></param>
        /// <returns></returns>
        public static bool EnableProgram([param: NotNull] Func<Func<bool>, string, string, bool> checkResult)
        {
            if (checkResult is null)
            {
                throw new ArgumentNullException(nameof(checkResult));
            }
            LogHelper.Debug("EnableProgram");
            if (IsTaskInstalled())
            {
                RemoveTask();  // will be re-created below
            }

            if (Settings.Default.StartNotifierAfterLogin)
            {
                if (!checkResult(() => createTask(), Resources.MSG_INST_OK, Resources.MSG_INST_TASK_ERR)) return false;
            }

            if (!checkResult(() => (ProcessHelper.getProcessFeedback(
                    Environment.SystemDirectory
                    + "\\reg.exe", @"ADD HKLM\SYSTEM\CurrentControlSet\Control\Lsa /v SCENoApplyLegacyAuditPolicy /t REG_DWORD /d 1 /f"))
                , "Registry enable SCENoApplyLegacyAuditPolicy."
                , Resources.MSG_INST_ENABLE_LOG_ERR)) return false;

            if (!checkResult(() => SetAuditPolConnection(enableSuccess: Settings.Default.AuditPolEnableSuccessEvent, enableFailure: true)
                , "Audit policy enabled."
                , Resources.MSG_INST_ENABLE_LOG_ERR)) return false;

            if (!checkResult(() => FirewallHelper.EnableWindowsFirewall()
                , "Windows firewall enabled."
                , Resources.MSG_INST_ENABLE_FW_ERR)) return false;

            if (!checkResult(() => CreateDefaultRules()
                , Resources.MSG_INST_OK
                , "Unable to create the default windows firewall rules.")) return false;

            Settings.Default.IsInstalled = true;
            Settings.Default.Save();

            return true;
        }

        public static bool SetAuditPolConnection(bool enableSuccess, bool enableFailure)
        {
            string successOption = enableSuccess ? "/success:enable" : "/success:disable";
            string failureOption = enableFailure ? "/failure:enable" : "/failure:disable";
            return ProcessHelper.getProcessFeedback(Environment.SystemDirectory + "\\auditpol.exe", "/set /subcategory:{0CCE9226-69AE-11D9-BED3-505054503030} " + successOption + " " + failureOption);
        }
        private static bool CreateDefaultRules()
        {
            bool ret = true;
            var rules = FirewallHelper.GetRules();
            using (ServiceController sc = new ServiceController())
            {
                string rname;

                // Windows 8 or higher
                if (Environment.OSVersion.Version >= new System.Version(6, 2))
                {
                    rname = String.Format(Resources.RULE_NAME_FORMAT, "Windows Applications (auto)");
                    if (rules.All(r => r.Name != rname))
                    {
                        FirewallHelper.CustomRule newRule = new FirewallHelper.CustomRule(rname, Environment.SystemDirectory + "\\wwahost.exe", null, null, (string)null, (int)FirewallHelper.Protocols.ANY, null, null, null, FirewallHelper.GetGlobalProfile(), CustomRuleAction.A);
                        ret = ret && newRule.Apply(false);
                    }
                }

                sc.ServiceName = "wuauserv";
                rname = String.Format(Resources.RULE_NAME_FORMAT, sc.DisplayName + " (auto)");
                if (rules.All(r => r.Name != rname + " [R:80,443]"))
                {
                    FirewallHelper.CustomRule newRule = new FirewallHelper.CustomRule(rname, Environment.SystemDirectory + "\\svchost.exe", null, null, "wuauserv", (int)FirewallHelper.Protocols.TCP, null, "80,443", null, FirewallHelper.GetGlobalProfile(), CustomRuleAction.A);
                    ret = ret && newRule.Apply(false);
                }

                sc.ServiceName = "bits";
                rname = String.Format(Resources.RULE_NAME_FORMAT, sc.DisplayName + "(auto)");
                if (rules.All(r => r.Name != rname + " [R:80,443]"))
                {
                    FirewallHelper.CustomRule newRule = new FirewallHelper.CustomRule(rname, Environment.SystemDirectory + "\\svchost.exe", null, null, "bits", (int)FirewallHelper.Protocols.TCP, null, "80,443", null, FirewallHelper.GetGlobalProfile(), CustomRuleAction.A);
                    ret = ret && newRule.Apply(false);
                }

                sc.ServiceName = "cryptsvc";
                rname = String.Format(Resources.RULE_NAME_FORMAT, sc.DisplayName + "(auto)");
                if (rules.All(r => r.Name != rname + " [R:80]"))
                {
                    FirewallHelper.CustomRule newRule = new FirewallHelper.CustomRule(rname, Environment.SystemDirectory + "\\svchost.exe", null, null, "cryptsvc", (int)FirewallHelper.Protocols.TCP, null, "80", null, FirewallHelper.GetGlobalProfile(), CustomRuleAction.A);
                    ret = ret && newRule.Apply(false);
                }

                //sc.ServiceName = "aelookupsvc";
                //rname = String.Format(Resources.RULE_NAME_FORMAT, sc.DisplayName + "(auto)");
                //if (rules.All(r => r.Name != rname + " [R:80]"))
                //{
                //    FirewallHelper.CustomRule newRule = new FirewallHelper.CustomRule(rname, Environment.SystemDirectory + "\\svchost.exe", null, null,"aelookupsvc", (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP, null, "80", null, FirewallHelper.GetGlobalProfile(), "A");
                //    ret = ret && newRule.Apply(false);
                //}
            }

            return ret;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static bool createTask()
        {
            LogHelper.Debug("CreateTask for Notifier");
            string tmpXML = Path.GetTempFileName();
            string newtask;
            using (var taskStr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Wokhan.WindowsFirewallNotifier.Console.Resources.TaskTemplate.xml")))
            {
                string principle = "<UserId><![CDATA[" + WindowsIdentity.GetCurrent().Name + "]]></UserId>";
                string command = "\"" + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Notifier.exe") + "\"";
                string arguments = Settings.Default.StartNotifierMinimized ? "-minimized " + Settings.Default.StartNotifierMinimized : ""; // TODO: To be implemented
                string dateTime = DateTime.Now.ToString("s");
                newtask = String.Format(taskStr.ReadToEnd(),
                                        principle,
                                        command,
                                        arguments,
                                        dateTime);
            }

            File.WriteAllText(tmpXML, newtask, Encoding.Unicode);

            bool ret = ProcessHelper.getProcessFeedback(Environment.SystemDirectory + "\\schtasks.exe", "/IT /Create /TN WindowsFirewallNotifierTask /XML \"" + tmpXML + "\"");

            File.Delete(tmpXML);

            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static bool RemoveTask()
        {
            LogHelper.Debug("RemoveTask for Notifier");
            if (IsTaskInstalled())
            {
                return ProcessHelper.getProcessFeedback(Environment.SystemDirectory + "\\schtasks.exe", "/Delete /TN WindowsFirewallNotifierTask /F");
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Is the task installed.
        /// </summary>
        /// <returns></returns>
        public static bool IsTaskInstalled()
        {
            return ProcessHelper.getProcessFeedback(Environment.SystemDirectory + "\\schtasks.exe", "/Query /TN WindowsFirewallNotifierTask");
        }

        public static bool IsInstalled()
        {
            return Settings.Default.IsInstalled;
        }

    }
}
