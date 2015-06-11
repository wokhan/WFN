using System;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Windows;
using Wokhan.WindowsFirewallNotifier.Console.Properties;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using System.Xml.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.XPath;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers
{
    public class InstallHelper
    {
        /// <summary>
        /// 
        /// </summary>
        public static bool RemoveProgram(bool reallowOutgoing, bool disableLogging, Action<string, string> failureCallback, Action<string, string> successCallback)
        {
            if (reallowOutgoing && !FirewallHelper.RestoreWindowsFirewall())
            {
                failureCallback(Resources.MSG_UNINST_UNBLOCK_ERR, Resources.MSG_DLG_ERR_TITLE);
                return false;
            }

            if (disableLogging
                && !ProcessHelper.getProcessFeedback(Environment.SystemDirectory + "\\auditpol.exe", "/set /subcategory:{0CCE9226-69AE-11D9-BED3-505054503030} /failure:disable"))
            {
                failureCallback(Resources.MSG_UNINST_DISABLE_LOG_ERR, Resources.MSG_DLG_ERR_TITLE);
                return false;
            }

            if (!RemoveTask())
            {
                failureCallback(Resources.MSG_UNINST_TASK_ERR, Resources.MSG_DLG_ERR_TITLE);
                return false;
            }

            successCallback(Resources.MSG_UNINST_OK, Resources.MSG_DLG_TITLE);

            return true;
        }


        /// <summary>
        /// 
        /// </summary>
        public static bool EnableProgram()
        {
            return EnableProgram(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pass"></param>
        /// <returns></returns>
        public static bool EnableProgram(bool allUsers)
        {
            if (IsInstalled())
            {
                RemoveTask();
            }

            if (ProcessHelper.getProcessFeedback(Environment.SystemDirectory + "\\reg.exe", @"ADD HKLM\SYSTEM\CurrentControlSet\Control\Lsa /v SCENoApplyLegacyAuditPolicy /t REG_DWORD /d 1 /f")
                && ProcessHelper.getProcessFeedback(Environment.SystemDirectory + "\\auditpol.exe", "/set /subcategory:{0CCE9226-69AE-11D9-BED3-505054503030} /failure:enable /success:disable"))
            {
                if (FirewallHelper.EnableWindowsFirewall())
                {
                    if (FirewallHelper.CreateDefaultRules())
                    {
                        if (createTask(allUsers))
                        {
                            MessageBox.Show(Resources.MSG_INST_OK, Resources.MSG_DLG_TITLE, MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show(Resources.MSG_INST_TASK_ERR, Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Unable to create the default rules, please consider reactivating Windows Firewall Notifier.", Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
                else
                {
                    MessageBox.Show(Resources.MSG_INST_ENABLE_FW_ERR, Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            else
            {
                MessageBox.Show(Resources.MSG_INST_ENABLE_LOG_ERR, Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static bool createTask(bool allUsers)
        {
            string tmpXML = Path.GetTempFileName();
            var taskStr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Wokhan.WindowsFirewallNotifier.Console.Resources.TaskTemplate.xml"));
            var newtask = String.Format(taskStr.ReadToEnd(),
                                        allUsers ? "<UserId>NT AUTHORITY\\SYSTEM</UserId>"//"<GroupId>S-1-5-32-545</GroupId>" 
                                                 : "<UserId><![CDATA[" + WindowsIdentity.GetCurrent().Name + "]]></UserId>",
                                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Notifier.exe"),
                                        DateTime.Now.ToString("s"));

            taskStr.Close();
                              
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
            return ProcessHelper.getProcessFeedback(Environment.SystemDirectory + "\\schtasks.exe", "/Delete /TN WindowsFirewallNotifierTask /F");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool IsInstalled()
        {
            return ProcessHelper.getProcessFeedback(Environment.SystemDirectory + "\\schtasks.exe", "/Query /TN WindowsFirewallNotifierTask");
        }

    }
}
