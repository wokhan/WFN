using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using WindowsFirewallNotifier;
using WindowsFirewallNotifierConsole.Extensions;
using WindowsFirewallNotifierConsole.Properties;

namespace WindowsFirewallNotifierConsole
{
    public partial class OptionsForm : Form
    {
        private IOrderedEnumerable<FirewallHelper.Rule> allrules;
        private DataTable dtableExceptions;
        private FileSystemWatcher fsWatcher = null;
        private bool initLogDone = false;
        private bool initExceptionsDone = false;
        private bool initRulesDone = false;

        public OptionsForm()
        {
            InitializeComponent();

            this.Size = Settings.Default.ConsoleSize;
            this.WindowState = Settings.Default.ConsoleState;

            txtFilter.KeyUp += new KeyEventHandler(txtFilter_KeyUp);

            lstConnections.SetDoubleBuffered();

            gridRules.AutoGenerateColumns = false;
            gridRules.SelectionChanged += new EventHandler(gridRules_SelectionChanged);
            gridExceptions.SelectionChanged += new EventHandler(gridExceptions_SelectionChanged);
            gridLog.SelectionChanged += new EventHandler(gridLog_SelectionChanged);

            colIcon.CellTemplate.Style.Padding = new Padding(2);
            colExcIcon.CellTemplate.Style.Padding = new Padding(2);

            dtableExceptions = new DataTable();
            dtableExceptions.Columns.Add("Icon", typeof(Image));
            dtableExceptions.Columns.Add("Path", typeof(string));
            dtableExceptions.Columns.Add("LocalPort", typeof(string));
            dtableExceptions.Columns.Add("Target", typeof(string));
            dtableExceptions.Columns.Add("RemotePort", typeof(string));

            gridExceptions.AutoGenerateColumns = false;
            gridExceptions.DataSource = dtableExceptions;
            gridExceptions.DataError += new DataGridViewDataErrorEventHandler(gridExceptions_DataError);

            tabPanel.SelectedIndexChanged += new EventHandler(tabPanel_SelectedIndexChanged);

            this.Icon = Resources.ICON_SHIELD;

            Settings.Default.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Default_PropertyChanged);
            if (Settings.Default.UseBlockRules)
            {
                btnTExceptions.Visible = false;
                btnTLog.Left = btnTExceptions.Left;
            }

            this.Show();

            this.Activate();

            // Update the connections tab once
            timerTrk_Tick(null, null);

            if (Settings.Default.FirstRun)
            {
                btnConnStop_Click(null, null);

                using (InstallForm insForm = new InstallForm())
                {
                    insForm.ShowDialog();
                }

                btnConnTrack_Click(null, null);

                Settings.Default.FirstRun = false;
                Settings.Default.Save();
            }
        }

        private void Default_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (Settings.Default.UseBlockRules)
            {
                btnTExceptions.Visible = false;
                btnTLog.Left = btnTExceptions.Left;
            }
            else
            {
                btnTExceptions.Visible = true;
                btnTExceptions.Left = 229;
                btnTLog.Left = 341;
            }
        }

        private void tabPanel_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (tabPanel.SelectedIndex)
            {
                case 0:
                    if (btnConnStop.Enabled)
                    {
                        enableTracking();
                    }
                    break;

                case 1:
                    disableTracking();
                    if (!initRulesDone)
                    {
                        initAllRules();
                        initRules();
                        initRulesDone = true;
                    }
                    break;

                case 2:
                    disableTracking();
                    if (!initExceptionsDone)
                    {
                        initExceptions();
                        initExceptionsDone = true;
                    }
                    break;

                case 3:
                    disableTracking();
                    if (!initLogDone)
                    {
                        initEventLog();
                        initLogDone = true;
                    }
                    break;

                default:
                    disableTracking();
                    break;
            }
        }

        private void enableTracking()
        {
            timerTrk.Enabled = true;
        }

        private void disableTracking()
        {
            timerTrk.Enabled = false;
        }

        private void gridExceptions_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtFilter_KeyUp(object sender, KeyEventArgs e)
        {
            initRules();
        }

        /// <summary>
        /// 
        /// </summary>
        private void initAllRules()
        {
            try
            {
                allrules = FirewallHelper.GetRules().OrderBy(r => r.Name);
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to load all FW rules", e);
            }
        }

        private void initRules()
        {
            try
            {
                Predicate<FirewallHelper.Rule> pred = null;
                if (activeRulesOnlyToolStripMenuItem.Checked)
                {
                    pred += activeRulesPredicate;
                }
                else if (wFNRulesOnlyToolStripMenuItem.Checked)
                {
                    pred += WFNRulesPredicate;
                }
                else if (wSHRulesOnlyToolStripMenuItem.Checked)
                {
                    pred += WSHRulesPredicate;
                }

                if (txtFilter.Text.Length > 0)
                {
                    pred += filteredRulesPredicate;
                }

                gridRules.DataSource = (pred == null ? allrules : allrules.Where(r => pred.GetInvocationList().All(p => ((Predicate<FirewallHelper.Rule>)p)(r)))).ToArray();
                //foreach (FirewallHelper.Rule r in res)
                //{
                //    gridRules.Rows.Add(r.Name, r.Icon, r.ApplicationName, r.Direction, r.Profile, r.Action, r.LocalPort, r.RemoteAddresses, r.RemotePorts, r.Enabled);
                //}
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to filter FW rules", e);
            }
        }

        private static string rulePrefix = WindowsFirewallNotifier.Properties.Resources.RULE_NAME_FORMAT.Split('-')[0];
        private bool WFNRulesPredicate(FirewallHelper.Rule r)
        {
            return r.Name.StartsWith(rulePrefix);
        }

        private bool WSHRulesPredicate(FirewallHelper.Rule r)
        {
            return r.Name.StartsWith("WSH -");
        }

        private bool activeRulesPredicate(FirewallHelper.Rule r)
        {
            return r.Enabled;
        }

        private bool filteredRulesPredicate(FirewallHelper.Rule r)
        {
            return (r.Name.IndexOf(txtFilter.Text, StringComparison.CurrentCultureIgnoreCase) > -1 || (r.ApplicationName != null && r.ApplicationName.IndexOf(txtFilter.Text, StringComparison.CurrentCultureIgnoreCase) > -1));
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void gridRules_SelectionChanged(object sender, EventArgs e)
        {
            if (gridRules.SelectedRows.Count == 0)
            {
                btnRDelete.Enabled = false;
                btnRLocate.Enabled = false;
            }
            else
            {
                btnRDelete.Enabled = !((FirewallHelper.Rule)gridRules.SelectedRows[0].DataBoundItem).Name.StartsWith("WSH -");
                btnRLocate.Enabled = !String.IsNullOrEmpty(((FirewallHelper.Rule)gridRules.SelectedRows[0].DataBoundItem).ApplicationName);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void initEventLog()
        {
            try
            {
                using (EventLog securityLog = new EventLog("security"))
                {
                    int i = securityLog.Entries.Count - 1;
                    int cpt = 500;
                    EventLogEntry entry;
                    string friendlyPath;
                    while (i > 0 && cpt > 0)
                    {
                        entry = securityLog.Entries[i--];
                        if (entry.InstanceId == 5157 && entry.EntryType == EventLogEntryType.FailureAudit)
                        {
                            cpt--;
                            friendlyPath = CommonHelper.GetFriendlyPath(entry.ReplacementStrings[1]);
                            gridLog.Rows.Add(entry.TimeWritten, ProcessHelper.GetIcon(friendlyPath, true), friendlyPath, entry.ReplacementStrings[5], getProtocol(entry.ReplacementStrings[7]), entry.ReplacementStrings[6]);

                            Application.DoEvents();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to load the event log", e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private string getProtocol(string p)
        {
            try
            {
                switch (int.Parse(p))
                {
                    case 6:
                        return "TCP";

                    case 17:
                        return "UDP";

                    default:
                        return "Other";
                }
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void initExceptions()
        {
            try
            {
                gridExceptions.DataSource = null;

                if (fsWatcher != null)
                {
                    fsWatcher.EnableRaisingEvents = false;
                }

                dtableExceptions.Rows.Clear();

                string path = Path.GetDirectoryName(Application.ExecutablePath) + "\\exclusions.set";
                if (File.Exists(path))
                {
                    foreach (string[] e in File.ReadAllLines(path).Select(s => s.Split(';')))
                    {
                        dtableExceptions.Rows.Add(new object[] { ProcessHelper.GetIcon(e[0], true) }.Concat(e).ToArray());
                    }

                    gridExceptions.DataSource = dtableExceptions;

                    if (fsWatcher == null)
                    {
                        fsWatcher = new FileSystemWatcher(Path.GetDirectoryName(Application.ExecutablePath));
                        fsWatcher.Filter = "exclusions.set";
                        fsWatcher.SynchronizingObject = gridExceptions;
                        fsWatcher.NotifyFilter = NotifyFilters.LastWrite;
                        fsWatcher.Changed += new FileSystemEventHandler(ExclusionsFile_Changed);
                    }

                    fsWatcher.EnableRaisingEvents = true;
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to initialize the exceptions list", e);
            }
        }

        private void ExclusionsFile_Changed(object sender, FileSystemEventArgs e)
        {
            initExceptions();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void gridExceptions_SelectionChanged(object sender, EventArgs e)
        {
            if (gridExceptions.SelectedRows.Count == 0 || gridExceptions.SelectedRows[0].IsNewRow)
            {
                btnELocate.Enabled = false;
                btnERemove.Enabled = false;
            }
            else
            {
                btnELocate.Enabled = true;
                btnERemove.Enabled = true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void gridLog_SelectionChanged(object sender, EventArgs e)
        {
            if (gridLog.SelectedRows.Count > 0)
            {
                DataGridViewRow drow = gridLog.SelectedRows[0];
                string path = (string)drow.Cells["colPath"].Value;
                if (path == "System")
                {
                    btnLLocate.Enabled = false;
                }
                else
                {
                    btnLLocate.Enabled = true;
                }

            }
            else
            {
                btnLLocate.Enabled = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveAll()
        {
            StreamWriter sw = null;

            try
            {
                if (this.WindowState != FormWindowState.Maximized)
                {
                    Settings.Default.ConsoleSize = this.Size;
                }

                Settings.Default.ConsoleState = this.WindowState;

                Settings.Default.Save();

                sw = new StreamWriter(Path.GetDirectoryName(Application.ExecutablePath) + "\\exclusions.set", false);

                foreach (DataRow drow in dtableExceptions.Rows)
                {
                    if (!String.IsNullOrEmpty((string)drow["Path"]))
                    {
                        sw.WriteLine(String.Join(";", drow.ItemArray.Skip(1).Select(r => r == DBNull.Value ? "" : (string)r).ToArray()));
                    }
                }
            }
            catch (Exception exc)
            {
                LogHelper.Error("Unable to save the configuration.", exc);
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }

                this.Close();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://wokhan.online.fr");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void btnLAllowPort_Click(object sender, EventArgs e)
        //{
        //    DataGridViewRow drow = gridLog.SelectedRows[0];

        //    string path = (string)drow.Cells["colPath"].Value;
        //    string targetPort = (string)drow.Cells["colPort"].Value;
        //    int protocol = (int)drow.Cells["colProtocol"].Value;

        //    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(path);

        //    if (FirewallHelper.AddRule(String.Format(Resources.RULE_NAME_FORMAT, fvi.FileDescription), path, null, protocol, targetPort))
        //    {
        //        MessageBox.Show(Resources.MSG_RULE_CREATED, Resources.MSG_DLG_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
        //    }
        //    else
        //    {
        //        MessageBox.Show(Resources.MSG_RULE_FAILED, Resources.MSG_DLG_ERR_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLLocate_Click(object sender, EventArgs e)
        {
            string path = (string)gridLog.SelectedRows[0].Cells["colPath"].Value;

            Process.Start("explorer.exe", "/select," + path);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnELocate_Click(object sender, EventArgs e)
        {
            string path = (string)gridExceptions.SelectedRows[0].Cells["colExcPath"].Value;

            Process.Start("explorer.exe", "/select," + path);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnERemove_Click(object sender, EventArgs e)
        {
            gridExceptions.Rows.Remove(gridExceptions.SelectedRows[0]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOpenConsole_Click(object sender, EventArgs e)
        {
            Process.Start("WF.msc");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRLocate_Click(object sender, EventArgs e)
        {
            string path = (string)gridRules.SelectedRows[0].Cells["colRPath"].Value;

            Process.Start("explorer.exe", "/select," + path);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRDelete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(Resources.MSG_RULE_DELETE, Resources.MSG_DLG_TITLE, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
            {
                string ruleName = (string)gridRules.SelectedRows[0].Cells["colRName"].Value;

                FirewallHelper.RemoveRule(ruleName);

                initAllRules();
                initRules();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLEvents_Click(object sender, EventArgs e)
        {
            Process.Start("eventvwr.msc");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void showAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnRShow.Text = showAllToolStripMenuItem.Text;
            activeRulesOnlyToolStripMenuItem.Checked = false;
            wFNRulesOnlyToolStripMenuItem.Checked = false;
            wSHRulesOnlyToolStripMenuItem.Checked = false;

            initRules();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void activeRulesOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnRShow.Text = activeRulesOnlyToolStripMenuItem.Text;
            showAllToolStripMenuItem.Checked = false;
            wFNRulesOnlyToolStripMenuItem.Checked = false;
            wSHRulesOnlyToolStripMenuItem.Checked = false;

            initRules();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void wFNRulesOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnRShow.Text = wFNRulesOnlyToolStripMenuItem.Text;
            activeRulesOnlyToolStripMenuItem.Checked = false;
            showAllToolStripMenuItem.Checked = false;
            wSHRulesOnlyToolStripMenuItem.Checked = false;

            initRules();
        }


        private void wSHRulesOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnRShow.Text = wSHRulesOnlyToolStripMenuItem.Text;
            activeRulesOnlyToolStripMenuItem.Checked = false;
            showAllToolStripMenuItem.Checked = false;
            wFNRulesOnlyToolStripMenuItem.Checked = false;

            initRules();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRRefresh_Click(object sender, EventArgs e)
        {
            initAllRules();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void btnLAllow_ButtonClick(object sender, EventArgs e)
        //{
        //    string path = (string)gridLog.SelectedRows[0].Cells["colPath"].Value;
        //    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(path);

        //    if (FirewallHelper.AddRule(String.Format(Resources.RULE_NAME_FORMAT, fvi.FileDescription), path, null))
        //    {
        //        MessageBox.Show(Resources.MSG_RULE_CREATED, Resources.MSG_DLG_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
        //    }
        //    else
        //    {
        //        MessageBox.Show(Resources.MSG_RULE_FAILED, Resources.MSG_DLG_ERR_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }

        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        //protected override void WndProc(ref System.Windows.Forms.Message m)
        //{
        //    if (m.Msg == 0x004A)
        //    {
        //        if (Program.mainForm != null && !Program.mainForm.IsDisposed && Program.mainForm.Visible)
        //        {
        //            ReflectMessage(Program.mainForm.Handle, ref m);
        //        }
        //        else
        //        {
        //            Program.COPYDATASTRUCT res = (Program.COPYDATASTRUCT)m.GetLParam(typeof(Program.COPYDATASTRUCT));

        //            string[] allres = res.lpData.Split(new string[] { "#$#" }, StringSplitOptions.None);

        //            Program.mainForm = new MainForm(int.Parse(allres[0]), allres[1], allres[2], allres[3], allres[4], allres[5]);
        //            if (!Program.mainForm.IsDisposed)
        //            {
        //                Program.mainForm.Show();
        //            }
        //        }
        //    }

        //    base.WndProc(ref m);
        //}


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timerTrk_Tick(object sender, EventArgs e)
        {
            lstConnections.BeginUpdate();

            ListViewItem lvi;

            foreach (var b in IpHlpApiHelper.GetAllTCPConnections())
            {
                lvi = LstConnections_AddItem(b, b.OwningPid, b.OwningPid + "TCP" + b.LocalPort, b.OwnerModule, "TCP", b.LocalAddress, b.LocalPort.ToString(), b.RemoteAddress, b.RemotePort.ToString(), Enum.GetName(typeof(IpHlpApiHelper.MIB_TCP_STATE), b.State), (b.CreationTime == DateTime.MinValue ? String.Empty : b.CreationTime.ToString()));
            }

            foreach (var b in IpHlpApiHelper.GetAllUDPConnections())
            {
                lvi = LstConnections_AddItem(b, b.OwningPid, b.OwningPid + "UDP" + b.LocalPort, b.OwnerModule, "UDP", b.LocalAddress, b.LocalPort.ToString(), String.Empty, String.Empty, String.Empty, (b.CreationTime == DateTime.MinValue ? String.Empty : b.CreationTime.ToString()));
            }

            if (Socket.OSSupportsIPv6)
            {
                foreach (var b in IpHlpApiHelper.GetAllTCP6Connections())
                {
                    lvi = LstConnections_AddItem(b, b.OwningPid, b.OwningPid + "TCP" + b.LocalPort, b.OwnerModule, "TCP", b.LocalAddress, b.LocalPort.ToString(), b.RemoteAddress, b.RemotePort.ToString(), Enum.GetName(typeof(IpHlpApiHelper.MIB_TCP_STATE), b.State), (b.CreationTime == DateTime.MinValue ? String.Empty : b.CreationTime.ToString()));
                }

                foreach (var b in IpHlpApiHelper.GetAllUDP6Connections())
                {
                    lvi = LstConnections_AddItem(b, b.OwningPid, b.OwningPid + "UDP" + b.LocalPort, b.OwnerModule, "UDP", b.LocalAddress, b.LocalPort.ToString(), String.Empty, String.Empty, String.Empty, (b.CreationTime == DateTime.MinValue ? String.Empty : b.CreationTime.ToString()));
                }
            }

            foreach (ListViewItem item in lstConnections.Items)
            {
                double elapsed = DateTime.Now.Subtract((DateTime)item.Tag).TotalSeconds;
                if (elapsed > 5)
                {
                    lstConnections.Items.Remove(item);
                }
                else if (elapsed > 2)
                {
                    item.BackColor = Color.Orange;
                }
            }

            lstConnections.EndUpdate();
        }

        private ListViewItem LstConnections_AddItem(IpHlpApiHelper.OWNER_MODULE b, uint pid, string key, IpHlpApiHelper.Owner owner, params string[] subitems)
        {
            ListViewItem lvi = lstConnections.Items[key];
            string ownerStr = (owner == null ? String.Empty : owner.ModuleName);

            if (lvi != null)
            {
                if (DateTime.Now.Subtract((DateTime)lvi.Tag).TotalMilliseconds > 500)
                {
                    lvi.BackColor = Color.Transparent;
                }

                lvi.Tag = DateTime.Now;
                lvi.Text = ownerStr;
                for (int i = 0; i < subitems.Length; i++)
                {
                    lvi.SubItems[i + 1].Text = subitems[i];
                }
            }
            else
            {
                string path;
                string procname;

                if (pid != 0 && pid != 4)
                {
                    using (Process proc = Process.GetProcessById((int)pid))
                    {
                        procname = proc.ProcessName;
                        try
                        {
                            path = proc.MainModule.FileName;
                        }
                        catch
                        {
                            path = String.Empty;
                        }
                    }
                }
                else
                {
                    path = "System";
                    procname = "System";
                }

                if (!imgLstConn.Images.ContainsKey(procname))
                {
                    imgLstConn.Images.Add(procname, ProcessHelper.GetIcon(path));
                }

                ListViewGroup grp = lstConnections.Groups[pid.ToString()];
                if (grp == null)
                {
                    grp = lstConnections.Groups.Add(pid.ToString(), String.Format("{0} ({1}) - [{2}]", procname, path, pid.ToString()));
                    grp.Tag = path;
                }

                lvi = lstConnections.Items.Add(new ListViewItem() { Name = key, Text = ownerStr, ImageKey = procname, Tag = DateTime.Now, BackColor = Color.LightBlue, Group = grp });
                lvi.SubItems.AddRange(subitems);
            }

            return lvi;
        }

        private void btnConnTrack_Click(object sender, EventArgs e)
        {
            timerTrk.Enabled = true;
            btnConnTrack.Enabled = false;
            btnConnStop.Enabled = true;
        }

        private void btnConnStop_Click(object sender, EventArgs e)
        {
            timerTrk.Enabled = false;
            btnConnTrack.Enabled = true;
            btnConnStop.Enabled = false;
        }


        private void btnConnFindR_Click(object sender, EventArgs e)
        {
            ListViewItem lvi = lstConnections.SelectedItems[0];
            if (lvi != null)
            {
                if (!initRulesDone)
                {
                    initAllRules();
                    initRules();

                    initRulesDone = true;
                }

                DataGridViewRow row = gridRules.Rows.Cast<DataGridViewRow>()
                                                    .FirstOrDefault(r => FirewallHelper.RuleMatches((FirewallHelper.Rule)r.DataBoundItem, (string)lvi.Group.Tag, lvi.SubItems[0].Text, lvi.SubItems[1].Text, lvi.SubItems[3].Text, lvi.SubItems[5].Text));

                if (row != null)
                {
                    tabPanel.SelectedTab = tabRules;
                    row.Selected = true;
                }
                else
                {
                    MessageBox.Show("No rule matches the current connection.", "No rule found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void normal2sToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fast1sToolStripMenuItem.Checked = false;
            slow5SecondsToolStripMenuItem.Checked = false;
            timerTrk.Interval = 1000;
        }

        private void slow5SecondsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fast1sToolStripMenuItem.Checked = false;
            normal2sToolStripMenuItem.Checked = false;
            timerTrk.Interval = 5000;
        }

        private void fast1sToolStripMenuItem_Click(object sender, EventArgs e)
        {
            normal2sToolStripMenuItem.Checked = false;
            slow5SecondsToolStripMenuItem.Checked = false;
            timerTrk.Interval = 500;
        }

        private void btnOptions_Click(object sender, EventArgs e)
        {
            using (InstallForm insForm = new InstallForm())
            {
                insForm.ShowDialog();
            }
        }

        private void btnDonate_Click(object sender, EventArgs e)
        {
            using (AboutForm aForm = new AboutForm())
            {
                aForm.ShowDialog();
            }
        }

        private void btnTConnections_Click(object sender, EventArgs e)
        {
            btnTConnections.BackColor = Color.LightBlue;
            btnTLog.BackColor = Color.White;
            btnTRules.BackColor = Color.White;
            btnTExceptions.BackColor = Color.White;

            tabPanel.SelectTab(tabConnections);
        }

        private void btnTRules_Click(object sender, EventArgs e)
        {
            btnTConnections.BackColor = Color.White;
            btnTLog.BackColor = Color.White;
            btnTRules.BackColor = Color.LightBlue;
            btnTExceptions.BackColor = Color.White;

            tabPanel.SelectTab(tabRules);
        }

        private void btnTLog_Click(object sender, EventArgs e)
        {
            btnTConnections.BackColor = Color.White;
            btnTLog.BackColor = Color.LightBlue;
            btnTRules.BackColor = Color.White;
            btnTExceptions.BackColor = Color.White;

            tabPanel.SelectTab(tabLog);
        }

        private void btnTExceptions_Click(object sender, EventArgs e)
        {
            btnTConnections.BackColor = Color.White;
            btnTExceptions.BackColor = Color.LightBlue;
            btnTLog.BackColor = Color.White;
            btnTRules.BackColor = Color.White;

            tabPanel.SelectTab(tabExceptions);
        }

        private void OptionsForm_Load(object sender, EventArgs e)
        {
            try
            {
                WebClient wc = new WebClient();
                string updateStr = wc.DownloadString("http://wokhan.online.fr/progs/" + Resources.FILENAME_VERSION);
                if (Version.Parse(updateStr) > Version.Parse(Application.ProductVersion))
                {
                    btnUpdate.Visible = true;
                }
            }
            catch { }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            new UpdateForm().ShowDialog();
        }
    }

}
