using System;
using System.IO;
using System.Security.Principal;
using System.Text;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Common.Properties;
using System.Reflection;
using System.ServiceProcess;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Wokhan.WindowsFirewallNotifier.Common.Config;
using Wokhan.WindowsFirewallNotifier.Common.Net.WFP;
using Wokhan.WindowsFirewallNotifier.Common.Net.WFP.Rules;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers
{
    public class InstallHelper
    {

        internal const String NOTIFIER_TASK_NAME = "WindowsFirewallNotifierTask";

        /// <summary>
        /// Check install after save.
        /// </summary>
        public static bool InstallCheck(Func<Func<bool>, string, string, bool> checkResult)
        {
            LogHelper.Debug(nameof(InstallCheck));
            if (checkResult is null)
            {
                throw new ArgumentNullException(nameof(checkResult));
            }

            if (!checkResult(() => SetAuditPolConnection(enableSuccess: Settings.Default.AuditPolEnableSuccessEvent, enableFailure: true)
                , "Security log audit policy enabled."
                , Resources.MSG_INST_ENABLE_LOG_ERR)) return false;

            if (Settings.Default.StartNotifierAfterLogin)
            {
                if (!checkResult(() => CreateNotifierTask(), $"{Resources.MSG_INST_OK} Notitifer auto-start after login enabled.", Resources.MSG_INST_TASK_ERR)) return false;
            }
            else
            {
                if (!checkResult(() => RemoveNotifierTask(), $"{Resources.MSG_INST_OK} Notifier auto-start disabled.", Resources.MSG_UNINST_TASK_ERR)) return false;
            }

            Settings.Default.Save();

            return true;
        }


        /// <summary>
        /// Install and setup.
        /// </summary>
        /// <param name="checkResult"></param>
        /// <returns></returns>
        public static bool Install([param: NotNull] Func<Func<bool>, string, string, bool> checkResult)
        {
            if (checkResult is null)
            {
                throw new ArgumentNullException(nameof(checkResult));
            }
            LogHelper.Debug("EnableProgram");
            if (IsNotifierTaskInstalled())
            {
                RemoveNotifierTask();  // will be re-created below
            }

            if (Settings.Default.StartNotifierAfterLogin)
            {
                if (!checkResult(() => CreateNotifierTask(), "Notifier will start after next windows login", Resources.MSG_INST_TASK_ERR)) return false;
            }

            if (!checkResult(() => (ProcessHelper.GetProcessFeedback(
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
            return ProcessHelper.GetProcessFeedback(Environment.SystemDirectory + "\\auditpol.exe", "/set /subcategory:{0CCE9226-69AE-11D9-BED3-505054503030} " + successOption + " " + failureOption);
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
                        CustomRule newRule = new CustomRule(rname, Environment.SystemDirectory + "\\wwahost.exe", null, null, (string)null, Protocol.ANY, null, null, null, FirewallHelper.GetGlobalProfile(), CustomRule.CustomRuleAction.A);
                        ret = ret && FirewallHelper.AddRule(newRule.GetPreparedRule(false));
                    }
                }

                sc.ServiceName = "wuauserv";
                rname = String.Format(Resources.RULE_NAME_FORMAT, sc.DisplayName + " (auto)");
                if (rules.All(r => r.Name != rname + " [R:80,443]"))
                {
                    CustomRule newRule = new CustomRule(rname, Environment.SystemDirectory + "\\svchost.exe", null, null, "wuauserv", Protocol.TCP, null, "80,443", null, FirewallHelper.GetGlobalProfile(), CustomRule.CustomRuleAction.A);
                    ret = ret && FirewallHelper.AddRule(newRule.GetPreparedRule(false));
                }

                sc.ServiceName = "bits";
                rname = String.Format(Resources.RULE_NAME_FORMAT, sc.DisplayName + "(auto)");
                if (rules.All(r => r.Name != rname + " [R:80,443]"))
                {
                    CustomRule newRule = new CustomRule(rname, Environment.SystemDirectory + "\\svchost.exe", null, null, "bits", Protocol.TCP, null, "80,443", null, FirewallHelper.GetGlobalProfile(), CustomRule.CustomRuleAction.A);
                    ret = ret && FirewallHelper.AddRule(newRule.GetPreparedRule(false));
                }

                sc.ServiceName = "cryptsvc";
                rname = String.Format(Resources.RULE_NAME_FORMAT, sc.DisplayName + "(auto)");
                if (rules.All(r => r.Name != rname + " [R:80]"))
                {
                    CustomRule newRule = new CustomRule(rname, Environment.SystemDirectory + "\\svchost.exe", null, null, "cryptsvc", Protocol.TCP, null, "80", null, FirewallHelper.GetGlobalProfile(), CustomRule.CustomRuleAction.A);
                    ret = ret && FirewallHelper.AddRule(newRule.GetPreparedRule(false));
                }

                //sc.ServiceName = "aelookupsvc";
                //rname = String.Format(Resources.RULE_NAME_FORMAT, sc.DisplayName + "(auto)");
                //if (rules.All(r => r.Name != rname + " [R:80]"))
                //{
                //    CustomRule newRule = new CustomRule(rname, Environment.SystemDirectory + "\\svchost.exe", null, null,"aelookupsvc", (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP, null, "80", null, FirewallHelper.GetGlobalProfile(), "A");
                //    ret = ret && FirewallHelper.AddRule(newRule.GetPreparedRule(false));
                //}
            }

            return ret;
        }


        /// <summary>
        /// Create the notifier task.
        /// </summary>
        /// <returns></returns>
        public static bool CreateNotifierTask()
        {
            LogHelper.Debug("CreateNotifierTask");
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

            bool ret = ProcessHelper.GetProcessFeedback(Environment.SystemDirectory + "\\schtasks.exe", $"/IT /Create /TN {NOTIFIER_TASK_NAME} /XML \"" + tmpXML + "\"");

            File.Delete(tmpXML);

            ret = RunNotifierTask();

            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static bool RemoveNotifierTask()
        {
            LogHelper.Debug("RemoveNotifierTask");
            if (IsNotifierTaskInstalled())
            {
                return ProcessHelper.GetProcessFeedback(Environment.SystemDirectory + "\\schtasks.exe", $"/Delete /TN {NOTIFIER_TASK_NAME} /F");
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Ends the notifier task (if installed and running)
        /// </summary>
        /// <returns></returns>
        public static bool EndNotifierTask()
        {
            LogHelper.Debug("EndNotifierTask");
            return ProcessHelper.GetProcessFeedback(Environment.SystemDirectory + "\\schtasks.exe", $"/End /TN {NOTIFIER_TASK_NAME}");
        }

        /// <summary>
        /// Runs the notifier task immediately (if installed)
        /// </summary>
        /// <returns></returns>
        public static bool RunNotifierTask()
        {
            LogHelper.Debug("RunNotifierTask");
            return ProcessHelper.GetProcessFeedback(Environment.SystemDirectory + "\\schtasks.exe", $"/Run /I /TN {NOTIFIER_TASK_NAME}");
        }


        /// <summary>
        /// Gets whether the notifier task is installed.
        /// </summary>
        /// <returns></returns>
        public static bool IsNotifierTaskInstalled()
        {
            bool isInstalled = ProcessHelper.GetProcessFeedback(Environment.SystemDirectory + "\\schtasks.exe", $"/Query /TN {NOTIFIER_TASK_NAME}");
            LogHelper.Debug($"IsNotifierTaskInstalled {isInstalled}");
            return isInstalled;
        }

        /// <summary>
        /// Gets the fllag whether the app was installed from settings.
        /// </summary>
        /// <returns></returns>
        public static bool IsInstalled()
        {
            LogHelper.Debug($"IsInstalled: {Settings.Default.IsInstalled}");
            return Settings.Default.IsInstalled;
        }

    }
}
