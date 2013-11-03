using System.Collections.Generic;
using WindowsFirewallNotifier;
using System.Windows.Forms;
using System.Diagnostics;
using System.Linq;
using System;
using System.Drawing;
using System.Text;
using WindowsFirewallNotifier.Properties;

namespace WindowsFirewallNotifier.RuleManager
{
    class Program
    {
        private static string path;
        private static string tmpname;
        
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                bool keepOpen = false;
                string[] param = Encoding.Unicode.GetString(Convert.FromBase64String(args[0])).Split(new string[] { "#$#" }, StringSplitOptions.None);

                string rname = param[0];
                path = param[1];
                string service = param[2];
                int protocol = int.Parse(param[3]);
                string target = param[4];
                string targetPort = param[5];
                string localPort = param[6];
                bool useCurrentProfile = bool.Parse(param[7]);
                string action = param[8];
                bool ret = false;

                switch (action)
                {
                    case "A":
                        ret = FirewallHelper.AddAllowRule(rname, path, service, protocol, target, targetPort, localPort, useCurrentProfile);
                        break;

                    case "B":
                        ret = FirewallHelper.AddBlockRule(rname, path, service, protocol, target, targetPort, localPort, useCurrentProfile);
                        break;

                    case "T":
                        tmpname = "[WFN Temp Rule] " + Guid.NewGuid().ToString();
                        ret = FirewallHelper.AddTempRule(tmpname, path, service, protocol, target, targetPort, localPort, useCurrentProfile);
                        keepOpen = true;
                        break;
                }

                if (!ret)
                {
                    throw new Exception("Unable to create the rule");
                }
                else if (keepOpen)
                {
                    NotifyIcon ni = new NotifyIcon();
                    ni.Click += new EventHandler(ni_Click);
                    ni.BalloonTipIcon = ToolTipIcon.Info;
                    ni.BalloonTipTitle = "Temporary rule";
                    ni.BalloonTipText = "A temporary rule has been created.\r\nPath: " + param[8] + "\r\nClick on the shield icon to disable back this connection.";
                    ni.Icon = new Icon(SystemIcons.Shield, new Size(16, 16));
                    ni.Visible = true;
                    ni.ShowBalloonTip(2000);

                    Application.Run();
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("WFNRuleManager failure", e);
                Environment.Exit(1);
            }

            Environment.Exit(0);
        }

        private static void ni_Click(object sender, EventArgs e)
        {
            if (!FirewallHelper.RemoveRule(tmpname))
            {
                MessageBox.Show(Resources.MSG_RULE_RM_FAILED, Resources.MSG_DLG_ERR_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Environment.Exit(0);
        }

    }
}
