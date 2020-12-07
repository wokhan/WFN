using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using Wokhan.WindowsFirewallNotifier.Notifier.Helpers;
using WinForms = System.Windows.Forms;
using Messages = Wokhan.WindowsFirewallNotifier.Common.Properties.Resources;
using System.Drawing;
using Wokhan.WindowsFirewallNotifier.Common.Net.WFP;
using Wokhan.WindowsFirewallNotifier.Common.Net.WFP.Rules;
using Wokhan.WindowsFirewallNotifier.Common.Config;
using Wokhan.WindowsFirewallNotifier.Common.Processes;
using Wokhan.WindowsFirewallNotifier.Common.Logging;

namespace Wokhan.WindowsFirewallNotifier.Notifier.UI.Windows
{
    /**
    * Interaction logic for NotificationWindow.xaml
    * 
    * authors: 
    *   harrwiss
    *   wokhan
    */
    public partial class NotificationWindow : System.Windows.Window, INotifyPropertyChanged
    {
        private bool isDetailsExpanded;

        private readonly NotifierTrayIcon notifierTrayIcon;

        public double ExpectedTop => SystemParameters.WorkArea.Height - this.ActualHeight;

        public double ExpectedLeft => SystemParameters.WorkArea.Width - this.ActualWidth;

        public double ExpectedWidth => SystemParameters.WorkArea.Width - this.ExpectedLeft;

        public int NbConnectionsAfter => lstConnections?.SelectedIndex >= 0 ? lstConnections.Items.Count - lstConnections.SelectedIndex - 1 : 0;
        public int NbConnectionsBefore => lstConnections?.SelectedIndex >= 0 ? lstConnections.SelectedIndex : 0;

        public class OptionsViewClass
        {
            public bool IsLocalPortChecked { get; set; }
            public bool IsTargetPortEnabled { get; set; }
            public bool IsTargetPortChecked { get; set; }
            public bool IsCurrentProfileChecked { get; set; }
            public bool IsServiceRuleChecked { get; set; }
            public bool IsTargetIPChecked { get; set; }
            public bool IsProtocolChecked { get; set; }
            public bool IsAppEnabled { get; set; }
            public bool IsAppChecked { get; set; }
            public bool IsService { get; set; }
            public bool IsServiceMultiple { get; set; }
            public string SingleServiceName { get; set; }
            public bool IsPathChecked { get; set; } = true;
        }

        public OptionsViewClass OptionsView { get; } = new OptionsViewClass();

        public static string CurrentProfile { get { return FirewallHelper.GetCurrentProfileAsText(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Initializes the form
        /// </summary>
        public NotificationWindow()
        {
            InitializeComponent();

            notifierTrayIcon = NotifierTrayIcon.Init(this);

            isDetailsExpanded = expand.IsExpanded;
            
            if (Settings.Default.AccentColor != null)
            {
                Resources["AccentColorBrush"] = Settings.Default.AccentColor;
            }

            lstConnections.SelectionChanged += LstConnections_SelectionChanged;
            ((ObservableCollection<CurrentConn>)lstConnections.ItemsSource).CollectionChanged += NotificationWindow_CollectionChanged;
            lstConnections.SelectedIndex = 0;

            // Re-calculate ExpectedTop
            SizeChanged += (sender, args) => { Top = ExpectedTop; };

            NotifyPropertyChanged(nameof(NbConnectionsAfter));
            NotifyPropertyChanged(nameof(NbConnectionsBefore));
        }
       
        public new void Show()
        {
            Debug.WriteLine($"Show Top: {Top} ExpTop: {ExpectedTop}");

            base.Show();
            base.Activate();
            //Top = ExpectedTop;
        }

        public void RestoreWindowState()
        {
            Show(); // required to trigger state changed events
            WindowState = WindowState.Normal;
        }

        internal void HideWindowState()
        {
            Hide();
            ShowInTaskbar = false;
            WindowState = WindowState.Minimized;
            notifierTrayIcon.Show();
        }

        private void NotificationWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                //
            }
            else
            {
                //ShowInTaskbar = false;
            }
        }

        private void NotificationWindow_Initialized(object sender, EventArgs e)
        {
            if (Settings.Default.UseAnimation)
            {
                this.Margin = new Thickness(250, 0, -250, 0);
            }

            //TODO: implement detection for apps in fullscreen
            /*if (WindowHelper.isSomeoneFullscreen())
            {
                ShowActivated = false;
                Topmost = false;
            }*/
        }

        private void NotificationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Top = ExpectedTop;
            this.Left = ExpectedLeft;

            StartAnimation();
        }

        private void NotificationWindow_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            StopAnimation();
            this.Focus();  //  focus the element
        }

        private void StartAnimation()
        {
            if (Settings.Default.UseAnimation)
            {
                ((Storyboard)this.Resources["animate"]).Begin(Main, true);
            }

        }

        private void StopAnimation()
        {
            if (Settings.Default.UseAnimation)
            {
                ((Storyboard)this.Resources["animate"]).Stop(Main);
            }
            this.Opacity = 1.0; //@
        }

        private void NotificationWindow_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (lstConnections.Items.Count > 0 && lstConnections.SelectedItem is null)
            {
                lstConnections.SelectedIndex = 0;
            }

            NotifyPropertyChanged(nameof(NbConnectionsAfter));
            NotifyPropertyChanged(nameof(NbConnectionsBefore));
        }

        private void LstConnections_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstConnections.Items.Count > 0)
            {
                Title = String.Format(Messages.FW_TITLE, lstConnections.Items.Count);
                if (lstConnections.SelectedItem is null)
                {
                    lstConnections.SelectedIndex = 0;
                }
                showConn();
            }
            else
            {
                Title = Messages.FW_TITLE_NO_CONNECTION;
                // HideWindowState();
            }
            //Console.WriteLine($"-> lstConnections.SelectedIndex={lstConnections.SelectedIndex}");
            NotifyPropertyChanged(nameof(NbConnectionsAfter));
            NotifyPropertyChanged(nameof(NbConnectionsBefore));
        }

        /// <summary>
        /// Updates all controls contents according to the currently selected blocked connection
        /// </summary>
        public void showConn()
        {
            var activeConn = (CurrentConn)lstConnections.SelectedItem;

            if (Protocol.IsUnknown(activeConn.RawProtocol))
            {
                OptionsView.IsProtocolChecked = false;
            }
            else
            {
                //On by default. Also: needed to be able to specify port!
                OptionsView.IsProtocolChecked = true;
            }

            // By default one would usually make a rule on the target ip/port for outgoing connections
            OptionsView.IsLocalPortChecked = false;
            OptionsView.IsTargetPortEnabled = Protocol.IsIPProtocol(activeConn.RawProtocol);
            OptionsView.IsTargetPortChecked = Protocol.IsIPProtocol(activeConn.RawProtocol);
            OptionsView.IsTargetIPChecked = Protocol.IsIPProtocol(activeConn.RawProtocol);

            if (!String.IsNullOrEmpty(activeConn.CurrentService))
            {
                OptionsView.IsService = true;
                OptionsView.IsServiceMultiple = false;
                OptionsView.IsServiceRuleChecked = true;
                OptionsView.SingleServiceName = activeConn.CurrentServiceDesc;
            }
            else if (activeConn.PossibleServices != null && activeConn.PossibleServices.Length > 0)
            {
                OptionsView.IsService = true;
                if (activeConn.PossibleServices.Length > 1)
                {
                    OptionsView.IsServiceMultiple = true;
                    OptionsView.SingleServiceName = "";
                }
                else
                {
                    OptionsView.IsServiceMultiple = false;
                    OptionsView.SingleServiceName = activeConn.PossibleServicesDesc.FirstOrDefault();
                }
                OptionsView.IsServiceRuleChecked = false; //If we're unsure, let's choose the safe option. There are executables out there that run services but also open connections outside of those services. A false positive in such a case would create a rule that doesn't work.
            }
            else
            {
                OptionsView.IsService = false;
                OptionsView.IsServiceMultiple = false;
                OptionsView.IsServiceRuleChecked = false;
                OptionsView.SingleServiceName = "";
            }

            OptionsView.IsAppEnabled = !String.IsNullOrEmpty(activeConn.CurrentAppPkgId);

            NotifyPropertyChanged(nameof(OptionsView));
        }

        /// <summary>
        /// Creates an "always allow" rule for the current application (depending on selected information)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAllow_Click(object sender, RoutedEventArgs e)
        {
            createRule(true);
        }

        /// <summary>
        /// Creates a blocking rule for the current application (depending on selected information)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBlock_Click(object sender, RoutedEventArgs e) 
        {
            createRule(false);
        }

        /// <summary>
        /// Show the next connection attempt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            lstConnections.SelectedIndex = Math.Min(lstConnections.Items.Count - 1, lstConnections.SelectedIndex + 1);
            if (lstConnections.SelectedItem != null)
            {
                lstConnections.ScrollIntoView(lstConnections.SelectedItem);
            }
            //Console.WriteLine($"Next_click: SelectedIndex={lstConnections.SelectedIndex}");
        }

        /// <summary>
        /// Show the previous connection attempt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            lstConnections.SelectedIndex = lstConnections.SelectedIndex >= 0 ? lstConnections.SelectedIndex - 1 : -1;
            if (lstConnections.SelectedItem != null)
            {
                lstConnections.ScrollIntoView(lstConnections.SelectedItem);
            }
            //Console.WriteLine($"Prev_click: SelectedIndex={lstConnections.SelectedIndex}");
        }

        /// <summary>
        /// Minimizes the Notifier window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnMin_Click(object sender, RoutedEventArgs e)
        {
            HideWindowState();
        }

        private void lblService_Click(object sender, RoutedEventArgs e) //FIXME: Not referenced (anymore!)
        {
            ProcessHelper.StartShellExecutable("services.msc", null, true);
        }

        private void btnOptions_Click(object sender, RoutedEventArgs e)
        {
            ShowConsole();
        }

        public void ShowConsole()
        {
            if (Process.GetProcessesByName(ProcessNames.WFN.ProcessName).Length == 0)
            {
                Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProcessNames.WFN.FileName));
            }
            else
            {
                ProcessHelper.StartOrRestoreToForeground(ProcessNames.WFN);
            }
        }

        private void btnSkip_Click(object sender, RoutedEventArgs e)
        {
            var tmpSelectedItem = (CurrentConn)lstConnections.SelectedItem;
            if (tmpSelectedItem != null && lstConnections.Items.Count > 0)
            {
                lstConnections.SelectedIndex = Math.Max(-1, lstConnections.SelectedIndex);
                ((App)Application.Current).Connections.Remove(tmpSelectedItem);
            }
            //Console.WriteLine($"Skip_click: SelectedIndex={lstConnections.SelectedIndex}");
        }

        private void btnSkipProgram_Click(object sender, RoutedEventArgs e)
        {
            SkipAllEntriesFromPath(((CurrentConn)lstConnections.SelectedItem).Path);
        }

        private void SkipAllEntriesFromPath(string path)
        {
            var toRemove = lstConnections.Items.Cast<CurrentConn>()
                                               .Where(connection => connection.Path == path)
                                               .ToList();

            foreach (var connection in toRemove)
            {
                if (lstConnections.SelectedItem == connection)
                {
                    lstConnections.SelectedIndex = Math.Max(-1, lstConnections.SelectedIndex);
                }
                ((App)Application.Current).Connections.Remove(connection);
            }

            if (lstConnections.Items.Count == 0)
            {
                //this.Close();
                // HideWindowState();
            }
            //Console.WriteLine($"SkipProgram_click: SelectedIndex={lstConnections.SelectedIndex}");
        }

        public new void Close()
        {
            RemoveTempRulesAndNotfyIcon();
            notifierTrayIcon?.Dispose();
            base.Close();
        }

        private void btnSkipAll_Click(object sender, RoutedEventArgs e)
        {
            lstConnections.SelectedIndex = -1;
            ((App)Application.Current).Connections.Clear();
            HideWindowState();
        }


        // TODO: Replace with an handler on PropertyChanged event for Settings.Default.
        // BTW, all auto-saved settings should be managed in the Settings class (will do that later).
        private void expand_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //this.Top = ExpectedTop;
            //this.Left = ExpectedLeft;

            if (isDetailsExpanded != expand.IsExpanded)
            {
                isDetailsExpanded = expand.IsExpanded;
                try
                {
                    Settings.Default.Save();
                }
                catch (Exception exc)
                {
                    LogHelper.Error("Notification window settings cannot be saved.", exc);
                }
            }
        }

        private void createRule(bool doAllow)
        {
            var createTempRule = (bool)togTempRule.IsChecked;
            var createWithAdvancedOptions = !expand.IsExpanded;
            bool success;
            var activeConn = (CurrentConn)lstConnections.SelectedItem;
            if (activeConn is null)
            {
                return;
            }

            if ((!OptionsView.IsProtocolChecked) && (OptionsView.IsLocalPortChecked || OptionsView.IsTargetPortChecked))
            {
                MessageBox.Show(Messages.MSG_RULE_PROTOCOL_NEEDED, Messages.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string[] services = null;
            if (OptionsView.IsServiceRuleChecked)
            {
                if (activeConn.PossibleServices != null && activeConn.PossibleServices.Length > 0)
                {
                    ServicesForm sf = new ServicesForm(activeConn);
                    if (!(bool)sf.ShowDialog())
                    {
                        return;
                    }
                    services = sf.SelectedServices;
                }
                else
                {
                    services = new[] { activeConn.CurrentService };
                }
            }

            var ruleName = String.Format(Messages.RULE_NAME_FORMAT, activeConn.CurrentServiceDesc ?? activeConn.Description);
            if (doAllow)
            {
                success = createAllowRule(activeConn, services, createWithAdvancedOptions, createTempRule, ruleName);
            }
            else
            {
                success = createBlockRule(activeConn, services, createWithAdvancedOptions, createTempRule, ruleName);
            }

            if (success)
            {
                LogHelper.Info("New rule for connection successfully created!");
                if (!createWithAdvancedOptions)
                {
                    SkipAllEntriesFromPath(activeConn.Path);
                }
                else
                {
                    SkipAllEntriesFromRules();
                }

                if (((App)Application.Current).Connections.Count == 0)
                {
                    LogHelper.Debug("No connections left; closing notification window.");
                    HideWindowState();
                }
            }
            else
            {
                MessageBox.Show(Messages.MSG_RULE_FAILED, Messages.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void SkipAllEntriesFromRules()
        {
            for (int i = ((App)Application.Current).Connections.Count - 1; i >= 0; i--)
            {
                var c = ((App)Application.Current).Connections[i];
                if (FirewallHelper.GetMatchingRules(c.Path, c.CurrentAppPkgId, c.RawProtocol, c.TargetIP, c.TargetPort, c.SourcePort, c.CurrentService, c.CurrentLocalUserOwner, false).Any()) //FIXME: LocalPort may have multiple!)
                {
                    LogHelper.Debug("Auto-removing a similar connection...");
                    ((App)Application.Current).Connections.Remove(c);
                }
            }
        }

        private bool createBlockRule(CurrentConn activeConn, string[] services, bool createWithAdvancedOptions, bool createTempRule, string ruleName)
        {
            bool success;
            if (Settings.Default.UseBlockRules)
            {
                int Profiles = OptionsView.IsCurrentProfileChecked ? FirewallHelper.GetCurrentProfile() : FirewallHelper.GetGlobalProfile();
                string finalRuleName = (createTempRule) ? Messages.RULE_TEMP_PREFIX + ruleName : ruleName;
                var newRule = new CustomRule(finalRuleName,
                                             createWithAdvancedOptions || OptionsView.IsPathChecked ? activeConn.Path : null,
                                             !createWithAdvancedOptions && OptionsView.IsAppChecked ? activeConn.CurrentAppPkgId : null,
                                             activeConn.CurrentLocalUserOwner,
                                             services,
                                             !createWithAdvancedOptions && OptionsView.IsProtocolChecked ? activeConn.RawProtocol : -1,
                                             !createWithAdvancedOptions && OptionsView.IsTargetIPChecked ? activeConn.TargetIP : null,
                                             !createWithAdvancedOptions && OptionsView.IsTargetPortChecked ? activeConn.TargetPort : null,
                                             !createWithAdvancedOptions && OptionsView.IsLocalPortChecked ? activeConn.SourcePort : null,
                                             Profiles,
                                             CustomRule.CustomRuleAction.Block);
                success = FirewallHelper.AddRule(newRule.GetPreparedRule(createTempRule)); // does not use RuleManager
                if (success && createTempRule)
                {
                    CreateTempRuleNotifyIcon(newRule);
                }
                if (!success)
                {
                    MessageBox.Show(Messages.MSG_RULE_FAILED, Messages.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // FIXME: Remove and always use Global Rules?
                throw new ArgumentException("Only global block rules can be used - check options");

                string entry = (!OptionsView.IsServiceRuleChecked || String.IsNullOrEmpty(activeConn.CurrentService) ? activeConn.Path : activeConn.CurrentService) +
                               (OptionsView.IsLocalPortChecked ? ";" + activeConn.SourcePort : ";") +
                               (OptionsView.IsTargetIPChecked ? ";" + activeConn.TargetIP : ";") +
                               (OptionsView.IsTargetPortChecked ? ";" + activeConn.TargetPort : ";"); //FIXME: Need to add more?
                using (StreamWriter sw = new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exclusions.set"), true))
                {
                    sw.WriteLine(entry);
                }

                success = true;
            }

            return success;
        }

        private bool createAllowRule(CurrentConn activeConn, string[] services, bool createWithAdvancedOptions, bool createTempRule, string ruleName)
        {
            int Profiles = OptionsView.IsCurrentProfileChecked ? FirewallHelper.GetCurrentProfile() : FirewallHelper.GetGlobalProfile();
            string finalRuleName = createTempRule ? Messages.RULE_TEMP_PREFIX + ruleName : ruleName;
            var newRule = new CustomRule(finalRuleName,
                                         createWithAdvancedOptions || OptionsView.IsPathChecked ? activeConn.Path : null,
                                         !createWithAdvancedOptions && OptionsView.IsAppChecked ? activeConn.CurrentAppPkgId : null,
                                         activeConn.CurrentLocalUserOwner,
                                         services,
                                         !createWithAdvancedOptions && OptionsView.IsProtocolChecked ? activeConn.RawProtocol : -1,
                                         !createWithAdvancedOptions && OptionsView.IsTargetIPChecked ? activeConn.TargetIP : null,
                                         !createWithAdvancedOptions && OptionsView.IsTargetPortChecked ? activeConn.TargetPort : null,
                                         !createWithAdvancedOptions && OptionsView.IsLocalPortChecked ? activeConn.SourcePort : null,
                                         Profiles,
                                         CustomRule.CustomRuleAction.Allow);

            bool success = FirewallHelper.AddRule(newRule.GetPreparedRule(createTempRule)); // does not use RuleManager

            if (success && createTempRule)
            {
                CreateTempRuleNotifyIcon(newRule);
            }

            return success;
        }

        private static WinForms::NotifyIcon tempNotifyIcon_;
        private List<CustomRule> tempRules_ = new List<CustomRule>();
        private void CreateTempRuleNotifyIcon(CustomRule newRule)
        {
            if (!tempRules_.Contains(newRule))
            {
                tempRules_.Add(newRule);
            }
            if (tempNotifyIcon_ is null)
            {
                // tray icon for temporary rule
                WinForms::NotifyIcon ni = new WinForms::NotifyIcon();
                ni.Click += iconClick;
                // shown in message center on win10
                ni.BalloonTipIcon = WinForms::ToolTipIcon.Info;
                ni.BalloonTipTitle = Common.Properties.Resources.RULE_TEMP_TITLE;
                ni.BalloonTipText = Messages.RULE_TEMP_DESCRIPTION;
                // tooltip shown on tray icon
                ni.Text = Messages.RULE_TEMP_DESCRIPTION_SHORT;  // limit 64 chars on win10
                ni.Icon = new Icon(SystemIcons.Shield, new System.Drawing.Size(16, 16));
                ni.Visible = true;
                ni.ShowBalloonTip(2000);
                ni.Visible = true;

                tempNotifyIcon_ = ni;
            }
        }

        private void iconClick(object sender, EventArgs e)
        {
            if (!RemoveTempRulesAndNotfyIcon())
            {
                WinForms::MessageBox.Show(Messages.MSG_RULE_RM_FAILED, Messages.MSG_DLG_ERR_TITLE, WinForms::MessageBoxButtons.OK, WinForms::MessageBoxIcon.Error);
            }
        }

        private bool RemoveTempRulesAndNotfyIcon()
        {
            bool success = true;
            if (tempRules_.Count > 0)
            {
                LogHelper.Info("Now going to remove temporary rule(s)...");
                success = tempRules_.TrueForAll(r => FirewallHelper.RemoveRule(r.Name));
            }
            if (tempNotifyIcon_ != null)
            {
                tempNotifyIcon_.Dispose();
                tempNotifyIcon_ = null;
            }
            return success;
        }

        private void hlkPath_Navigate(object sender, RequestNavigateEventArgs e)
        {
            ProcessHelper.StartShellExecutable("explorer.exe", String.Format("/select,\"{0}\"", e.Uri), true);
        }

        private void hlk_Navigate(object sender, RequestNavigateEventArgs e)
        {
            ProcessHelper.StartShellExecutable(e.Uri?.AbsoluteUri, null, true);
            e.Handled = true;
        }

        private void NotifWindow_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {

        }
    }
}
