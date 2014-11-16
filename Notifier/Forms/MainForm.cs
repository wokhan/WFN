using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using WindowsFirewallNotifier.Forms;
using WindowsFirewallNotifier.Properties;

namespace WindowsFirewallNotifier
{
    public partial class MainForm : Form
    {
        public class CurrentConn
        {
            public string CurrentProd;
            public string CurrentPath;
            public string CurrentService;
            public string CurrentServiceDesc;
            public string RuleName;
            public string LocalPort;
            public string Target;
            public string TargetPort;
            public int Protocol;
            public string ResolvedHost;

            public string[] PossibleServices;
            public string[] PossibleServicesDesc;

        }

        private ToolTip ttip = new ToolTip();

        private List<CurrentConn> conns = new List<CurrentConn>();
        private CurrentConn activeConn;

        private string[] exclusions = null;

        private string defAppText;
        private string defPath;
        private string defLPort;
        private string defTarget;
        private string defTargetPort;
        private string defSvcText;

        /// <summary>
        /// Initializes the form
        /// </summary>
        /// <param name="app"></param>
        /// <param name="path"></param>
        /// <param name="target"></param>
        /// <param name="protocol"></param>
        /// <param name="targetPort"></param>
        public MainForm(int pid, string threadid, string path, string target, string protocol, string targetPort, string localPort)
        {
            initExclusions();

            if (!AddItem(pid, threadid, path, target, protocol, targetPort, localPort, false))
            {
                this.Close();
                this.Dispose();
                return;
            }

            InitializeComponent();

            {
                ComponentResourceManager resources = new ComponentResourceManager(typeof(MainForm));
                defAppText = resources.GetString(lblApp.Name + ".Text");
                defPath = resources.GetString(lblPath.Name + ".Text");
                defTarget = resources.GetString(chkTRule.Name + ".Text");
                defSvcText = resources.GetString(chkServiceRule.Name + ".Text");
                defLPort = resources.GetString(chkLPortRule.Name + ".Text");
                defTargetPort = resources.GetString(chkPortRule.Name + ".Text");
            }

            if (!Settings.Default.AlwaysShowDetails)
            {
                this.Height = btnAdvanced.Top + btnAdvanced.Height + 3;
            }

            chkCurrentProfile.Text = String.Format(chkCurrentProfile.Text, FirewallHelper.GetCurrentProfileAsText());


            this.Icon = Resources.ICON_SHIELD;
            this.Top = Screen.PrimaryScreen.WorkingArea.Bottom - this.Height;

            if (Settings.Default.UseAnimation)
            {
                this.Left = Screen.PrimaryScreen.WorkingArea.Right;
                this.Width = 0;
                this.Shown += new EventHandler(MainForm_Shown);
                this.FormClosing += MainForm_FormClosing;
            }
            else
            {
                this.Left = Screen.PrimaryScreen.WorkingArea.Width - this.Width - 5;
            }

            pnlMain.Height = this.Height;

            lblPath.MouseMove += new MouseEventHandler(lbl_MouseMove);
            lblPath.MouseLeave += new EventHandler(lbl_MouseLeave);
            lblPath.Click += new EventHandler(lblPath_Click);

            ttip.SetToolTip(btnAlwaysAllow, Resources.MSG_ALLOW);
            ttip.SetToolTip(btnAlwaysBlock, Resources.MSG_BLOCK);

            activeConn = conns[conns.Count - 1];
            showConn();
        }

        void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            animateLeft(false);

        }

        /// <summary>
        /// 
        /// </summary>
        private void initExclusions()
        {
            try
            {
                if (!Settings.Default.UseBlockRules)
                {
                    string exclusionsPath = Path.GetDirectoryName(Application.ExecutablePath) + "\\exclusions.set";
                    if (File.Exists(exclusionsPath))
                    {
                        exclusions = File.ReadAllLines(exclusionsPath);
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to load the exceptions list.", e);
            }
        }

        /// <summary>
        /// Adds an item to the currently blocked connections list
        /// </summary>
        /// <param name="app"></param>
        /// <param name="path"></param>
        /// <param name="target"></param>
        /// <param name="protocol"></param>
        /// <param name="targetPort"></param>
        public bool AddItem(int pid, string threadid, string path, string target, string protocol, string targetPort, string localport, bool updateCount)
        {
            try
            {
                if (path != "System")
                {
                    path = CommonHelper.GetFriendlyPath(path);
                }

                if (!this.conns.Any(c => c.CurrentPath == path && (int.Parse(localport) >= 49152 || c.LocalPort == localport))) //&& c.TargetPort == targetPort))
                {
                    string[] svc = new string[0];
                    string[] svcdsc = new string[0];
                    bool unsure = false;
                    string app = null;

                    if (path == "System")
                    {
                        app = "System";
                    }
                    else
                    {
                        try
                        {
                            app = FileVersionInfo.GetVersionInfo(path).FileDescription;
                        }
                        catch
                        {
                            app = String.Empty;
                        }

                        //if (Settings.Default.EnableServiceDetection)
                        {
                            ProcessHelper.GetService(pid, threadid, (NetFwTypeLib.NET_FW_IP_PROTOCOL_)Enum.Parse(typeof(NetFwTypeLib.NET_FW_IP_PROTOCOL_), protocol), targetPort, localport, out svc, out svcdsc, out unsure);
                        }
                    }

                    if (exclusions != null)
                    {
                        // WARNING: check for regressions
                        var exclusion = exclusions.FirstOrDefault(e => e.StartsWith(/*svc ??*/path, StringComparison.CurrentCultureIgnoreCase) || svc != null && svc.All(s => e.StartsWith(s, StringComparison.CurrentCultureIgnoreCase)));
                        if (exclusion != null)
                        {
                            string[] esplit = exclusion.Split(';');
                            if (esplit.Length == 1 ||
                                    ((esplit[1] == String.Empty || esplit[1] == localport) &&
                                     (esplit[2] == String.Empty || esplit[2] == target) &&
                                     (esplit[3] == String.Empty || esplit[3] == targetPort)))
                            {
                                return false;
                            }
                        }
                    }

                    // WARNING: check for regressions
                    if (FirewallHelper.GetRules()
                                      .Any(r => r.Action == NetFwTypeLib.NET_FW_ACTION_.NET_FW_ACTION_BLOCK &&
                                          (!unsure && FirewallHelper.RuleMatches(r, path, svc != null && svc.Length > 0 ? svc[0] : null, protocol, localport, targetPort))))
                    {
                        return false;
                    }

                    this.conns.Add(new CurrentConn
                    {
                        CurrentProd = app,
                        CurrentPath = path,
                        CurrentService = !unsure ? svc.FirstOrDefault() : null,
                        PossibleServices = unsure ? svc : null,
                        CurrentServiceDesc = !unsure ? svcdsc.FirstOrDefault() : null,
                        PossibleServicesDesc = unsure ? svcdsc : null,
                        Protocol = int.Parse(protocol),
                        TargetPort = targetPort,
                        RuleName = String.Format(WindowsFirewallNotifier.Properties.Resources.RULE_NAME_FORMAT, unsure || String.IsNullOrEmpty(svcdsc.FirstOrDefault()) ? app : svcdsc.FirstOrDefault()),
                        Target = target,
                        LocalPort = localport
                    });

                    if (updateCount)
                    {
                        updateCountLbl();
                    }

                    this.Activate();

                    return true;
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to add the connection to the pool.", e);
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        private void updateCountLbl()
        {
            int index = conns.IndexOf(activeConn);

            lblConn.Text = (index + 1) + "/" + conns.Count;
            //lblClose.Text = Resources.

            btnNext.Enabled = (index < (conns.Count - 1));
            btnPrev.Enabled = (index > 0);

        }

        /// <summary>
        /// Updates all controls contents according to the currently selected blocked connection
        /// </summary>
        private void showConn()
        {
            using (Icon ico = ProcessHelper.GetIcon(activeConn.CurrentPath))
            {
                pictureBox1.Image = ico.ToBitmap();
            }

            updateCountLbl();

            lblApp.Text = activeConn.CurrentProd;
            lblApp.Tag = lblApp.Text;

            lblPath.Text = String.Format(defPath, activeConn.CurrentPath);
            lblPath.Tag = activeConn.CurrentPath;

            if (!String.IsNullOrEmpty(activeConn.ResolvedHost))
            {
                chkTRule.Text = String.Format(defTarget, activeConn.Target + " (" + activeConn.ResolvedHost + ")");
            }
            else
            {
                chkTRule.Text = String.Format(defTarget, activeConn.Target);
                Dns.BeginGetHostEntry(activeConn.Target, GetHostEntryCallback, activeConn);
                chkTRule.Tag = activeConn.Target;
            }

            chkPortRule.Text = String.Format(defTargetPort, activeConn.TargetPort);
            chkPortRule.Tag = activeConn.TargetPort;

            chkLPortRule.Text = String.Format(defLPort, activeConn.LocalPort);
            chkLPortRule.Tag = activeConn.LocalPort;

            chkTemp.Checked = false;

            chkTRule.Enabled = true;
            chkPortRule.Enabled = FirewallHelper.IsIPProtocol(activeConn.Protocol);
            chkLPortRule.Enabled = (int.Parse(activeConn.LocalPort) < 49152);

            chkPortRule.Checked = false;
            chkLPortRule.Checked = false;
            chkTRule.Checked = false;

            if (!String.IsNullOrEmpty(activeConn.CurrentService))
            {
                chkServiceRule.Enabled = true;
                chkServiceRule.Checked = true;
                chkServiceRule.ForeColor = Control.DefaultForeColor;
                chkServiceRule.Text = String.Format(defSvcText, activeConn.CurrentService + (String.IsNullOrEmpty(activeConn.CurrentServiceDesc) ? String.Empty : " (" + activeConn.CurrentServiceDesc + ")"));
            }
            else if (activeConn.PossibleServices != null && activeConn.PossibleServices.Length > 0)
            {
                chkServiceRule.Enabled = true;
                chkServiceRule.Checked = true;
                chkServiceRule.Text = String.Format(defSvcText, Resources.SERVICES_UNDEF);
                chkServiceRule.ForeColor = Color.Red;
            }
            else
            {
                chkServiceRule.Enabled = false;
                chkServiceRule.Checked = false;
                chkServiceRule.ForeColor = Control.DefaultForeColor;
                chkServiceRule.Text = String.Format(defSvcText, "-");
            }

            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // CS_DROPSHADOW
                //cp.ClassStyle |= 0x00020000;
                return cp;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ar"></param>
        private void GetHostEntryCallback(IAsyncResult ar)
        {
            try
            {
                string hostname = Dns.EndGetHostEntry(ar).HostName;
                if (hostname != activeConn.Target)
                {
                    ((CurrentConn)ar.AsyncState).ResolvedHost = hostname;
                    if (ar.AsyncState == activeConn)
                    {
                        chkTRule.Text = String.Format(defTarget, activeConn.Target + " (" + activeConn.ResolvedHost + ")");
                        chkTRule.Tag = activeConn.Target + " (" + activeConn.ResolvedHost + ")";
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Activates the form when it's displayed, and handles a simple animation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Shown(object sender, EventArgs e)
        {
            animateLeft(true);
        }

        private void animateLeft(bool show)
        {
            int startLeft = show ? Screen.PrimaryScreen.WorkingArea.Right : this.Left;
            int startWidth = show ? 0 : this.Width;
            int targetWidth = show ? pnlMain.Width + 3 : 0;
            int targetLeft = show ? Screen.PrimaryScreen.WorkingArea.Right - targetWidth : Screen.PrimaryScreen.WorkingArea.Right;
            double startOpacity = show ? 0 : 1;
            double targetOpacity = show ? 1 : 0;
            int i = 1;

            while (i < 20)
            {
                this.Opacity = CommonHelper.easeInOut(i, startOpacity, targetOpacity - startOpacity, 20);
                this.Width = (int)CommonHelper.easeInOut(i, startWidth, targetWidth - startWidth, 20);
                this.Left = (int)CommonHelper.easeInOut(i++, startLeft, targetLeft - startLeft, 20);

                Application.DoEvents();
                this.Invalidate();

                // I know, should rely on a timer for animations, feel free to change that one...
                Thread.Sleep(20);
            }

        }

        /// <summary>
        /// Opens the blocked application folder in explorer and selects the executable.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lblPath_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", "/select," + activeConn.CurrentPath);
        }

        /// <summary>
        /// Self explaining
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbl_MouseLeave(object sender, EventArgs e)
        {
            ((Control)sender).ForeColor = Color.Empty;
        }

        /// <summary>
        /// Self explaining
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbl_MouseMove(object sender, MouseEventArgs e)
        {
            ((Control)sender).ForeColor = Color.Blue;
        }


        /// <summary>
        /// Creates a rule for the current application (ALLOW)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAllow_Click(object sender, EventArgs e)
        {
            bool success = false;

            string[] services = null;
            if (chkServiceRule.Checked)
            {
                if (activeConn.PossibleServices != null && activeConn.PossibleServices.Length > 0)
                {
                    ServicesForm sf = new ServicesForm(activeConn);
                    sf.ShowDialog();
                    if (sf.DialogResult == System.Windows.Forms.DialogResult.OK)
                    {
                        services = sf.CreateAppRule ? null : sf.SelectedServices;
                        sf.Dispose();
                    }
                    else
                    {
                        sf.Dispose();
                        return;
                    }
                }
                else
                {
                    services = new[] { activeConn.CurrentService };
                }
            }

            if (chkTemp.Checked)
            {
                success = FirewallHelper.AddTempRuleIndirect(activeConn.RuleName, activeConn.CurrentPath, services, activeConn.Protocol, chkTRule.Checked ? activeConn.Target : null, chkPortRule.Checked ? activeConn.TargetPort : null, chkLPortRule.Checked ? activeConn.LocalPort : null, chkCurrentProfile.Checked);
            }
            else
            {
                success = FirewallHelper.AddAllowRuleIndirect(activeConn.RuleName, activeConn.CurrentPath, services, activeConn.Protocol, chkTRule.Checked ? activeConn.Target : null, chkPortRule.Checked ? activeConn.TargetPort : null, chkLPortRule.Checked ? activeConn.LocalPort : null, chkCurrentProfile.Checked);
            }

            if (success)
            {
                if (chkPortRule.Checked)
                {
                    conns.Remove(activeConn);
                }
                else
                {
                    conns.RemoveAll(c => c.CurrentPath == activeConn.CurrentPath);
                }

                if (conns.Count > 0)
                {
                    activeConn = conns[0];
                    showConn();
                }
                else
                {
                    this.Close();
                }
            }
            else
            {
                MessageBox.Show(chkTemp.Checked ? Resources.MSG_RULE_TMP_FAILED : Resources.MSG_RULE_FAILED, Resources.MSG_DLG_ERR_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        /// <summary>
        /// Adds the application to the exceptions list so that no further notifications will be displayed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnIgnore_Click(object sender, EventArgs e)
        {
            bool success = false;

            string[] services = null;
            if (chkServiceRule.Checked)
            {
                if (activeConn.PossibleServices != null && activeConn.PossibleServices.Length > 0)
                {
                    ServicesForm sf = new ServicesForm(activeConn);
                    sf.ShowDialog();
                    if (sf.DialogResult == System.Windows.Forms.DialogResult.OK)
                    {
                        services = sf.CreateAppRule ? null : sf.SelectedServices;
                        sf.Dispose();
                    }
                    else
                    {
                        sf.Dispose();
                        return;
                    }
                }
                else
                {
                    services = new[] { activeConn.CurrentService };
                }
            }

            if (!chkTemp.Checked)
            {
                if (Settings.Default.UseBlockRules)
                {
                    //Process.Start(new ProcessStartInfo(Application.ExecutablePath, ) { Verb = "runas" });
                    success = FirewallHelper.AddBlockRuleIndirect(activeConn.RuleName, activeConn.CurrentPath, services, activeConn.Protocol, chkTRule.Checked ? activeConn.Target : null, chkPortRule.Checked ? activeConn.TargetPort : null, chkLPortRule.Checked ? activeConn.LocalPort : null, chkCurrentProfile.Checked);
                    if (!success)
                    {
                        MessageBox.Show(Resources.MSG_RULE_FAILED, Resources.MSG_DLG_ERR_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    string entry = (!chkServiceRule.Checked || String.IsNullOrEmpty(activeConn.CurrentService) ? activeConn.CurrentPath : activeConn.CurrentService) +
                                   (chkLPortRule.Checked ? ";" + activeConn.LocalPort : ";") +
                                   (chkTRule.Checked ? ";" + activeConn.Target : ";") +
                                   (chkPortRule.Checked ? ";" + activeConn.TargetPort : ";");
                    StreamWriter sw = new StreamWriter(Path.GetDirectoryName(Application.ExecutablePath) + "\\exclusions.set", true);
                    sw.WriteLine(entry);
                    sw.Close();

                    success = true;
                }
            }

            if (success)
            {
                conns.RemoveAll(c => c.CurrentPath == activeConn.CurrentPath);

                if (conns.Count > 0)
                {
                    activeConn = conns[0];
                    showConn();
                }
                else
                {
                    this.Close();
                }
            }
        }

        /// <summary>
        /// Opens my website
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://wokhan.online.fr");
        }

        /// <summary>
        /// Stops temporarily allowing an application outgoing connections
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /*private void btnClose_Click(object sender, EventArgs e)
        {
            if (conns.Count > 1)
            {
                if (!FirewallHelper.RemoveRule("TEMP - " + activeConn.RuleName))
                {
                    MessageBox.Show(Resources.MSG_RULE_RM_FAILED, Resources.MSG_DLG_ERR_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                conns.Remove(activeConn);

                activeConn = conns[0];

                showConn();
            }
            else
            {
                this.Close();
            }
        }*/

        /// <summary>
        /// Quits
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            activeConn = conns[conns.IndexOf(activeConn) + 1];
            showConn();
        }

        private void btnPrev_Click(object sender, EventArgs e)
        {
            activeConn = conns[conns.IndexOf(activeConn) - 1];
            showConn();
        }

        private void btnMin_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void lblService_Click(object sender, EventArgs e)
        {
            Process.Start("services.msc");
        }

        private void btnOptions_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Console.exe"));
            }
            catch { }
        }
        
        private int exHeight = -1;
        private void btnAdvanced_Click(object sender, EventArgs e)
        {
            int targetHeight;
            int localexHeight = this.Height;
            if (exHeight == -1)
            {
                exHeight = this.Height;
                targetHeight = this.Height + pnlHeader.Height;
                pnlMain.Height = targetHeight;
            }
            else
            {
                targetHeight = exHeight;
                exHeight = -1;
            }

            int targetTop = Screen.PrimaryScreen.WorkingArea.Bottom - targetHeight;
            int startTop = this.Top;
            int i = 1;
            while (i < 20)
            {
                //this.Height += (targetHeight - localexHeight) / 20;// (int)QuadEaseInOut(targetHeight, exHeight, 2, 20);
                this.Height = (int)CommonHelper.easeInOut(i, localexHeight, targetHeight - localexHeight, 20);
                this.Top = (int)CommonHelper.easeInOut(i, startTop, targetTop - startTop, 20); ;

                Application.DoEvents();

                // I know, should rely on a timer for animations, feel free to change that one (yep, that one is a dup :-P)...
                Thread.Sleep(10);

                i++;
            }

            pnlMain.Height = targetHeight;
        }

        private void ctxtCopy_Click(object sender, EventArgs e)
        {
            var srccontrol = ((ContextMenuStrip)((ToolStripMenuItem)sender).Owner).SourceControl;
            var copiedValue = (string)(srccontrol.Tag ?? String.Empty);

            Clipboard.SetText(copiedValue);
        }

        private void btnNotifOpts_Click(object sender, EventArgs e)
        {

        }


    }
}
