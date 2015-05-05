using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wokhan.WindowsFirewallNotifier.Common;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Notifier.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Notifier.UI.Windows
{
    /// <summary>
    /// Interaction logic for NotificationWindow.xaml
    /// </summary>
    public partial class NotificationWindow : Window
    {
        public double StartLeft
        {
            get { return SystemParameters.PrimaryScreenWidth; }
        }

        public double ExpectedTop
        {
            get { return SystemParameters.PrimaryScreenHeight - this.Height; }
        }

        public double ExpectedLeft
        {
            get { return SystemParameters.PrimaryScreenWidth - this.Width; }
        }

        public class OptionsViewClass
        {
            public bool IsTempRuleChecked { get; set; }
            public bool IsLocalPortChecked { get; set; }
            public bool IsTargetPortChecked { get; set; }
            public bool IsCurrentProfileChecked { get; set; }
            public bool IsServiceRuleChecked { get; set; }
            public bool IsTargetIPChecked { get; set; }

        }

        private OptionsViewClass _optionsView = new OptionsViewClass();
        public OptionsViewClass OptionsView { get { return _optionsView; } }

        
        public string CurrentProfile { get { return FirewallHelper.GetCurrentProfileAsText(); } }


        private ToolTip ttip = new ToolTip();

        /// <summary>
        /// Initializes the form
        /// </summary>
        /// <param name="app"></param>
        /// <param name="path"></param>
        /// <param name="target"></param>
        /// <param name="protocol"></param>
        /// <param name="targetPort"></param>
        public NotificationWindow()
        {
            InitializeComponent();

            lstConnections.SelectedIndex = 0;

            this.Show();
            this.Activate();
            
            /*ttip.SetToolTip(btnAlwaysAllow, Resources.MSG_ALLOW);
            ttip.SetToolTip(btnAlwaysBlock, Resources.MSG_BLOCK);
            */
        }

        
        /// <summary>
        /// Updates all controls contents according to the currently selected blocked connection
        /// </summary>
        private void showConn()
        {
            //chkPortRule.Enabled = FirewallHelper.IsIPProtocol(activeConn.Protocol);
            //chkLPortRule.Enabled = (int.Parse(activeConn.LocalPort) < 49152);

            //if (!String.IsNullOrEmpty(activeConn.CurrentService))
            //{
            //    chkServiceRule.Enabled = true;
            //    chkServiceRule.Checked = true;
            //    chkServiceRule.ForeColor = Control.DefaultForeColor;
            //    chkServiceRule.Text = String.Format(defSvcText, activeConn.CurrentService + (String.IsNullOrEmpty(activeConn.CurrentServiceDesc) ? String.Empty : " (" + activeConn.CurrentServiceDesc + ")"));
            //}
            //else if (activeConn.PossibleServices != null && activeConn.PossibleServices.Length > 0)
            //{
            //    chkServiceRule.Enabled = true;
            //    chkServiceRule.Checked = true;
            //    chkServiceRule.Text = String.Format(defSvcText, Resources.SERVICES_UNDEF);
            //    chkServiceRule.ForeColor = Color.Red;
            //}
            //else
            //{
            //    chkServiceRule.Enabled = false;
            //    chkServiceRule.Checked = false;
            //    chkServiceRule.ForeColor = Control.DefaultForeColor;
            //    chkServiceRule.Text = String.Format(defSvcText, "-");
            //}

        }

        /// <summary>
        /// Opens the blocked application folder in explorer and selects the executable.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lblPath_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", "/select," + ((CurrentConn)lstConnections.SelectedItem).CurrentPath);
        }

        /// <summary>
        /// Creates a rule for the current application (ALLOW)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAllow_Click(object sender, RoutedEventArgs e)
        {
            bool success = false;
            var activeConn = ((CurrentConn)lstConnections.SelectedItem);

            string[] services = null;
            if (_optionsView.IsServiceRuleChecked)
            {
                if (activeConn.PossibleServices != null && activeConn.PossibleServices.Length > 0)
                {
                    //ServicesForm sf = new ServicesForm(activeConn);
                    //sf.ShowDialog();
                    //if (sf.DialogResult == System.Windows.Forms.DialogResult.OK)
                    //{
                    //    services = sf.CreateAppRule ? null : sf.SelectedServices;
                    //    sf.Dispose();
                    //}
                    //else
                    //{
                    //    sf.Dispose();
                    //    return;
                    //}
                }
                else
                {
                    services = new[] { activeConn.CurrentService };
                }
            }

            if (_optionsView.IsTempRuleChecked)
            {
                success = FirewallHelper.AddTempRuleIndirect(activeConn.RuleName, activeConn.CurrentPath, services, activeConn.Protocol, _optionsView.IsTargetIPChecked ? activeConn.Target : null, _optionsView.IsTargetPortChecked ? activeConn.TargetPort : null, _optionsView.IsLocalPortChecked ? activeConn.LocalPort : null, _optionsView.IsCurrentProfileChecked);
            }
            else
            {
                success = FirewallHelper.AddAllowRuleIndirect(activeConn.RuleName, activeConn.CurrentPath, services, activeConn.Protocol, _optionsView.IsTargetIPChecked ? activeConn.Target : null, _optionsView.IsTargetPortChecked ? activeConn.TargetPort : null, _optionsView.IsLocalPortChecked ? activeConn.LocalPort : null, _optionsView.IsCurrentProfileChecked);
            }

            if (success)
            {
                if (_optionsView.IsTargetPortChecked)
                {
                    ((App)Application.Current).Connections.Remove(activeConn);
                }
                else
                {
                    for (int i = ((App)Application.Current).Connections.Count; i > 0; i--)
                    {
                        var c = ((App)Application.Current).Connections[i];
                        if (c.CurrentPath == activeConn.CurrentPath)
                        {
                            ((App)Application.Current).Connections.Remove(c);
                        }
                    }
                }

                if (((App)Application.Current).Connections.Count == 0)
                {
                    this.Close();
                }
            }
            else
            {
                MessageBox.Show(_optionsView.IsTempRuleChecked ? Wokhan.WindowsFirewallNotifier.Common.Resources.MSG_RULE_TMP_FAILED : Wokhan.WindowsFirewallNotifier.Common.Resources.MSG_RULE_FAILED, Wokhan.WindowsFirewallNotifier.Common.Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// Adds the application to the exceptions list so that no further notifications will be displayed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnIgnore_Click(object sender, RoutedEventArgs e)
        {
            bool success = false;
            var activeConn = ((CurrentConn)lstConnections.SelectedItem);

            string[] services = null;
            if (_optionsView.IsServiceRuleChecked)
            {
                if (activeConn.PossibleServices != null && activeConn.PossibleServices.Length > 0)
                {
                    //ServicesForm sf = new ServicesForm(activeConn);
                    //sf.ShowDialog();
                    //if (sf.DialogResult == System.Windows.Forms.DialogResult.OK)
                    //{
                    //    services = sf.CreateAppRule ? null : sf.SelectedServices;
                    //    sf.Dispose();
                    //}
                    //else
                    //{
                    //    sf.Dispose();
                    //    return;
                    //}
                }
                else
                {
                    services = new[] { activeConn.CurrentService };
                }
            }

            if (!_optionsView.IsTempRuleChecked)
            {
                if (Settings.Default.UseBlockRules)
                {
                    //Process.Start(new ProcessStartInfo(Application.ExecutablePath, ) { Verb = "runas" });
                    success = FirewallHelper.AddBlockRuleIndirect(activeConn.RuleName, activeConn.CurrentPath, services, activeConn.Protocol, _optionsView.IsTargetIPChecked ? activeConn.Target : null, _optionsView.IsTargetPortChecked ? activeConn.TargetPort : null, _optionsView.IsLocalPortChecked ? activeConn.LocalPort : null, _optionsView.IsCurrentProfileChecked);
                    if (!success)
                    {
                        MessageBox.Show(Wokhan.WindowsFirewallNotifier.Common.Resources.MSG_RULE_FAILED, Wokhan.WindowsFirewallNotifier.Common.Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    string entry = (!_optionsView.IsServiceRuleChecked || String.IsNullOrEmpty(activeConn.CurrentService) ? activeConn.CurrentPath : activeConn.CurrentService) +
                                   (_optionsView.IsLocalPortChecked ? ";" + activeConn.LocalPort : ";") +
                                   (_optionsView.IsTargetIPChecked ? ";" + activeConn.Target : ";") +
                                   (_optionsView.IsTargetPortChecked ? ";" + activeConn.TargetPort : ";");
                    StreamWriter sw = new StreamWriter(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "exclusions.set"), true);
                    sw.WriteLine(entry);
                    sw.Close();

                    success = true;
                }
            }

            if (success)
            {
                for (int i = ((App)Application.Current).Connections.Count; i > 0; i--)
                {
                    var c = ((App)Application.Current).Connections[i];
                    if (c.CurrentPath == ((CurrentConn)lstConnections.SelectedItem).CurrentPath)
                    {
                        ((App)Application.Current).Connections.Remove(c);
                    }
                }

                if (((App)Application.Current).Connections.Count == 0)
                {
                    this.Close();
                }
            }
        }

        /// <summary>
        /// Quits
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            lstConnections.SelectedIndex++;
            lstConnections.ScrollIntoView(lstConnections.SelectedItem);
        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            lstConnections.SelectedIndex--;
            lstConnections.ScrollIntoView(lstConnections.SelectedItem);
        }

        private void btnMin_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        public void lblService_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("services.msc");
        }

        private void btnOptions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "WFN.exe"));
            }
            catch { }
        }

        //private int exHeight = -1;
        private void btnAdvanced_Expand(object sender, RoutedEventArgs e)
        {
            //int targetHeight;
            //int localexHeight = this.Height;
            //if (exHeight == -1)
            //{
            //    exHeight = this.Height;
            //    targetHeight = this.Height + pnlHeader.Height;
            //    pnlMain.Height = targetHeight;

            //    Settings.Default.AlwaysShowDetails = true;
            //}
            //else
            //{
            //    targetHeight = exHeight;
            //    exHeight = -1;

            //    Settings.Default.AlwaysShowDetails = false;
            //}

            //int targetTop = Screen.PrimaryScreen.WorkingArea.Bottom - targetHeight;
            //int startTop = this.Top;
            //int i = 1;
            //while (i < 20)
            //{
            //    //this.Height += (targetHeight - localexHeight) / 20;// (int)QuadEaseInOut(targetHeight, exHeight, 2, 20);
            //    this.Height = (int)CommonHelper.easeInOut(i, localexHeight, targetHeight - localexHeight, 20);
            //    this.Top = (int)CommonHelper.easeInOut(i, startTop, targetTop - startTop, 20); ;

            //    Application.DoEvents();

            //    // I know, should rely on a timer for animations, feel free to change that one (yep, that one is a dup :-P)...
            //    Thread.Sleep(10);

            //    i++;
            //}

            //pnlMain.Height = targetHeight;

            Settings.Default.Save();

        }

        private void ctxtCopy_Click(object sender, RoutedEventArgs e)
        {
            //var srccontrol = ((ContextMenuStrip)((ToolStripMenuItem)sender).Owner).SourceControl;
            //var copiedValue = (string)(srccontrol.Tag ?? String.Empty);

            //Clipboard.SetText(copiedValue);
        }

        private void btnSkipAll_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
