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
using Wokhan.WindowsFirewallNotifier.Common;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Common.Properties;
using Wokhan.WindowsFirewallNotifier.Notifier.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Notifier.UI.Windows
{
    /// <summary>
    /// Interaction logic for NotificationWindow.xaml
    /// </summary>
    public partial class NotificationWindow : Window, INotifyPropertyChanged
    {
        private bool isDetailsExpanded;

        public double ExpectedTop
        {
            get { return SystemParameters.WorkArea.Height - this.ActualHeight; }
        }

        public double ExpectedLeft
        {
            get { return SystemParameters.WorkArea.Width - this.ActualWidth; }
        }

        public int NbConnectionsAfter { get { return lstConnections != null ? lstConnections.Items.Count - lstConnections.SelectedIndex - 1 : 0; } }
        public int NbConnectionsBefore { get { return lstConnections != null ? lstConnections.SelectedIndex : 0; } }

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
        }

        private OptionsViewClass _optionsView = new OptionsViewClass();
        public OptionsViewClass OptionsView { get { return _optionsView; } }


        public string CurrentProfile { get { return FirewallHelper.GetCurrentProfileAsText(); } }


        //private ToolTip ttip = new ToolTip();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Initializes the form
        /// </summary>
        public NotificationWindow()
        {
            InitializeComponent();

            this.isDetailsExpanded = expand.IsExpanded;

            if (Settings.Default.AccentColor != null)
            {
                Resources["AccentColorBrush"] = Settings.Default.AccentColor;
            }

            lstConnections.SelectionChanged += LstConnections_SelectionChanged;
            ((ObservableCollection<CurrentConn>)lstConnections.ItemsSource).CollectionChanged += NotificationWindow_CollectionChanged;
            lstConnections.SelectedIndex = 0;

            //Make sure the showConn function is triggered on initial load.
            showConn();
            NotifyPropertyChanged("NbConnectionsAfter");
            NotifyPropertyChanged("NbConnectionsBefore");

            /*ttip.SetToolTip(btnAlwaysAllow, Resources.MSG_ALLOW);
            ttip.SetToolTip(btnAlwaysBlock, Resources.MSG_BLOCK);
            */
        }

        private void NotificationWindow_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("NbConnectionsAfter");
            NotifyPropertyChanged("NbConnectionsBefore");
        }

        private void NotificationWindow_Initialized(object sender, EventArgs e)
        {
            if (Settings.Default.UseAnimation)
            {
                this.Margin = new Thickness(250, 0, -250, 0);
            }

            if (WindowHelper.isSomeoneFullscreen())
            {
                ShowActivated = false;
                Topmost = false;
            }
        }

        private void NotificationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Top = ExpectedTop;
            this.Left = ExpectedLeft;

            if (Settings.Default.UseAnimation)
            {
                ((Storyboard)this.Resources["animate"]).Begin(Main, true);
            }
        }

        private void NotificationWindow_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (Settings.Default.UseAnimation)
            {
                ((Storyboard)this.Resources["animate"]).Stop(Main);
            }
            this.Opacity = 1.0; //@
        }

        private void LstConnections_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstConnections.Items.Count > 0)
            {
                if (lstConnections.SelectedItem == null)
                {
                    lstConnections.SelectedIndex = 0;
                }

                showConn();

                NotifyPropertyChanged("NbConnectionsAfter");
                NotifyPropertyChanged("NbConnectionsBefore");
            }
            else
            {
                this.Close();
            }
        }

        /// <summary>
        /// Updates all controls contents according to the currently selected blocked connection
        /// </summary>
        private void showConn()
        {
            var activeConn = (CurrentConn)lstConnections.SelectedItem;

            if (FirewallHelper.getProtocolAsString(activeConn.Protocol) == "Unknown") //FIXME: No string comparison, please!
            {
                OptionsView.IsProtocolChecked = false;
            }
            else
            {
                //On by default. Also: needed to be able to specify port!
                OptionsView.IsProtocolChecked = true;
            }
            OptionsView.IsTargetPortEnabled = FirewallHelper.IsIPProtocol(activeConn.Protocol);
            OptionsView.IsTargetPortChecked = FirewallHelper.IsIPProtocol(activeConn.Protocol);
            OptionsView.IsLocalPortChecked = (activeConn.LocalPortArray.Count == 1 && activeConn.LocalPortArray[0] != 0 && activeConn.LocalPortArray[0] < IPHelper.GetMaxUserPort());

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

            NotifyPropertyChanged("OptionsView");
        }

        /// <summary>
        /// Creates a rule for the current application (ALLOW)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAllow_Click(object sender, RoutedEventArgs e)
        {
            createRule(true, false);
        }

        private void btnAllowTemp_Click(object sender, RoutedEventArgs e)
        {
            createRule(true, true);
        }

        /// <summary>
        /// Adds the application to the exceptions list so that no further notifications will be displayed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnIgnore_Click(object sender, RoutedEventArgs e) //FIXME: Naming?
        {
            createRule(false, false);
        }

        private void btnBlockTemp_Click(object sender, RoutedEventArgs e)
        {
            createRule(false, true);
        }

        /// <summary>
        /// Show the next connection attempt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            lstConnections.SelectedIndex++;
            lstConnections.ScrollIntoView(lstConnections.SelectedItem);
        }

        /// <summary>
        /// Show the previous connection attempt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            lstConnections.SelectedIndex--;
            lstConnections.ScrollIntoView(lstConnections.SelectedItem);
        }

        /// <summary>
        /// Minimizes the Notifier window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnMin_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void lblService_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("services.msc");
        }

        private void btnOptions_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WFN.exe"));
        }

        private void ctxtCopy_Click(object sender, RoutedEventArgs e)
        {
            //var srccontrol = ((ContextMenuStrip)((ToolStripMenuItem)sender).Owner).SourceControl;
            //var copiedValue = (string)(srccontrol.Tag ?? String.Empty);

            //Clipboard.SetText(copiedValue);
        }

        private void btnSkip_Click(object sender, RoutedEventArgs e)
        {
            var tmpSelectedItem = (CurrentConn)lstConnections.SelectedItem;
            lstConnections.SelectedIndex -= 1;
            ((App)Application.Current).Connections.Remove(tmpSelectedItem);
        }

        private void btnSkipProgram_Click(object sender, RoutedEventArgs e)
        {
            String skipPath = ((CurrentConn)lstConnections.SelectedItem).CurrentPath;
            List<CurrentConn> toRemove = new List<CurrentConn>(); //Can't remove while iterating.
            foreach (var connection in lstConnections.Items)
            {
                if (((CurrentConn)connection).CurrentPath == skipPath)
                {
                    toRemove.Add((CurrentConn)connection);
                }
            }
            foreach (var connection in toRemove)
            {
                if (lstConnections.SelectedItem == connection)
                {
                    lstConnections.SelectedIndex -= 1;
                }
                ((App)Application.Current).Connections.Remove(connection);
            }
            if (lstConnections.Items.Count == 0)
            {
                this.Close();
            }
        }

        private void btnSkipAll_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void expand_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.Top = ExpectedTop;

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

        private void createRule(bool doAllow, bool isTemp)
        {
            bool success = false;
            var activeConn = ((CurrentConn)lstConnections.SelectedItem);

            if ((!_optionsView.IsProtocolChecked) && (_optionsView.IsLocalPortChecked || _optionsView.IsTargetPortChecked))
            {
                MessageBox.Show(Common.Properties.Resources.MSG_RULE_PROTOCOL_NEEDED, Common.Properties.Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string[] services = null;
            if (_optionsView.IsServiceRuleChecked)
            {
                if (activeConn.PossibleServices != null && activeConn.PossibleServices.Length > 0)
                {
                    ServicesForm sf = new ServicesForm(activeConn);
                    if ((bool)sf.ShowDialog())
                    {
                        services = sf.SelectedServices;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    services = new[] { activeConn.CurrentService };
                }
            }

            if (doAllow)
            {
                success = createAllowRule(activeConn, services, isTemp);
            }
            else
            {
                success = createBlockRule(activeConn, services, isTemp);
            }

            if (success)
            {
                LogHelper.Info("New rule for connection successfully created!");

                for (int i = ((App)Application.Current).Connections.Count - 1; i >= 0; i--)
                {
                    var c = ((App)Application.Current).Connections[i];
                    string[] svc = new string[0];
                    if (!String.IsNullOrEmpty(c.CurrentService))
                    {
                        svc = new[] { c.CurrentService };
                    }
                    if (FirewallHelper.GetMatchingRules(c.CurrentPath, c.CurrentAppPkgId, c.Protocol, c.Target, c.TargetPort, c.LocalPort, svc, c.CurrentLocalUserOwner, false).Any()) //FIXME: LocalPort may have multiple!)
                    {
                        LogHelper.Debug("Auto-removing a similar connection...");
                        ((App)Application.Current).Connections.Remove(c);
                    }
                }

                if (((App)Application.Current).Connections.Count == 0)
                {
                    LogHelper.Debug("No connections left; closing notification window.");
                    this.Close();
                }
            }
            else
            {
                MessageBox.Show(Common.Properties.Resources.MSG_RULE_FAILED, Common.Properties.Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool createBlockRule(CurrentConn activeConn, string[] services, bool isTemp)
        {
            bool success = false;
            if (!isTemp)
            {
                if (Settings.Default.UseBlockRules)
                {
                    int Profiles = _optionsView.IsCurrentProfileChecked ? FirewallHelper.GetCurrentProfile() : FirewallHelper.GetGlobalProfile();
                    FirewallHelper.CustomRule newRule = new FirewallHelper.CustomRule(activeConn.RuleName, activeConn.CurrentPath, _optionsView.IsAppChecked ? activeConn.CurrentAppPkgId : null, activeConn.CurrentLocalUserOwner, services, _optionsView.IsProtocolChecked ? activeConn.Protocol : -1, _optionsView.IsTargetIPChecked ? activeConn.Target : null, _optionsView.IsTargetPortChecked ? activeConn.TargetPort : null, _optionsView.IsLocalPortChecked ? activeConn.LocalPort : null, Profiles, "B");
                    success = newRule.ApplyIndirect(isTemp);
                    if (!success)
                    {
                        MessageBox.Show(Common.Properties.Resources.MSG_RULE_FAILED, Common.Properties.Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    string entry = (!_optionsView.IsServiceRuleChecked || String.IsNullOrEmpty(activeConn.CurrentService) ? activeConn.CurrentPath : activeConn.CurrentService) +
                                   (_optionsView.IsLocalPortChecked ? ";" + activeConn.LocalPort : ";") +
                                   (_optionsView.IsTargetIPChecked ? ";" + activeConn.Target : ";") +
                                   (_optionsView.IsTargetPortChecked ? ";" + activeConn.TargetPort : ";"); //FIXME: Need to add more?
                    using (StreamWriter sw = new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exclusions.set"), true))
                    {
                        sw.WriteLine(entry);
                    }

                    success = true;
                }
            }
            return success;
        }

        private bool createAllowRule(CurrentConn activeConn, string[] services, bool isTemp)
        {
            int Profiles = _optionsView.IsCurrentProfileChecked ? FirewallHelper.GetCurrentProfile() : FirewallHelper.GetGlobalProfile();
            FirewallHelper.CustomRule newRule = new FirewallHelper.CustomRule(activeConn.RuleName, activeConn.CurrentPath, _optionsView.IsAppChecked? activeConn.CurrentAppPkgId : null, activeConn.CurrentLocalUserOwner, services, _optionsView.IsProtocolChecked? activeConn.Protocol : -1, _optionsView.IsTargetIPChecked? activeConn.Target: null, _optionsView.IsTargetPortChecked? activeConn.TargetPort: null, _optionsView.IsLocalPortChecked? activeConn.LocalPort: null, Profiles, "A");
            return newRule.ApplyIndirect(isTemp);
        }

        private void hlkPath_Navigate(object sender, RequestNavigateEventArgs e)
        {
            LogHelper.Debug("Calling external program: explorer.exe /select,\"" + e.Uri + "\"");
            Process.Start("explorer.exe", String.Format("/select,\"{0}\"", e.Uri));
        }

        private void hlk_Navigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
