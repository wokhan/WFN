using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Wokhan.WindowsFirewallNotifier.Common;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Notifier.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Notifier.UI.Windows
{
    /// <summary>
    /// Interaction logic for NotificationWindow.xaml
    /// </summary>
    public partial class NotificationWindow : Window, INotifyPropertyChanged
    {
        public double StartLeft
        {
            get { return SystemParameters.WorkArea.Width; }
        }

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
            public bool IsTempRuleChecked { get; set; }
            public bool IsLocalPortChecked { get; set; }
            public bool IsTargetPortChecked { get; set; }
            public bool IsCurrentProfileChecked { get; set; }
            public bool IsServiceRuleChecked { get; set; }
            public bool IsTargetIPChecked { get; set; }
            public bool IsProtocolChecked { get; set; }
            public bool IsService { get; set; }
            public bool IsServiceMultiple { get; set; }
        }

        private OptionsViewClass _optionsView = new OptionsViewClass();
        public OptionsViewClass OptionsView { get { return _optionsView; } }


        public string CurrentProfile { get { return FirewallHelper.GetCurrentProfileAsText(); } }


        private ToolTip ttip = new ToolTip();

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

            if (Settings.Default.AccentColor != null)
            {
                Resources["AccentColorBrush"] = Settings.Default.AccentColor;
            }

            lstConnections.SelectionChanged += LstConnections_SelectionChanged;
            ((ObservableCollection<CurrentConn>)lstConnections.ItemsSource).CollectionChanged += NotificationWindow_CollectionChanged;
            lstConnections.SelectedIndex = 0;

            this.Loaded += NotificationWindow_Loaded;

            this.Show();
            //this.Activate();

            /*ttip.SetToolTip(btnAlwaysAllow, Resources.MSG_ALLOW);
            ttip.SetToolTip(btnAlwaysBlock, Resources.MSG_BLOCK);
            */
        }

        private void NotificationWindow_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("NbConnectionsAfter");
            NotifyPropertyChanged("NbConnectionsBefore");
        }

        private void LstConnections_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstConnections.SelectedItem == null && lstConnections.Items.Count > 0)
            {
                lstConnections.SelectedIndex = 0;
            }

            showConn();

            NotifyPropertyChanged("NbConnectionsAfter");
            NotifyPropertyChanged("NbConnectionsBefore");
        }


        private void NotificationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Top = ExpectedTop;
            this.Left = ExpectedLeft;

            Dispatcher.InvokeAsync(() => ((Storyboard)this.Resources["animate"]).Begin(Main));
        }


        /// <summary>
        /// Updates all controls contents according to the currently selected blocked connection
        /// </summary>
        private void showConn()
        {
            var activeConn = (CurrentConn)lstConnections.SelectedItem;

            chkPort.IsEnabled = FirewallHelper.IsIPProtocol(activeConn.Protocol);
            chkLPort.IsEnabled = (int.Parse(activeConn.LocalPort) < 49152);

            if (!String.IsNullOrEmpty(activeConn.CurrentService))
            {
                OptionsView.IsService = true;
                OptionsView.IsServiceMultiple = false;
                OptionsView.IsServiceRuleChecked = true;
            }
            else if (activeConn.PossibleServices != null && activeConn.PossibleServices.Length > 0)
            {
                OptionsView.IsService = true;
                OptionsView.IsServiceMultiple = true;
                OptionsView.IsServiceRuleChecked = true;
            }
            else
            {
                OptionsView.IsService = false;
                OptionsView.IsServiceMultiple = false;
                OptionsView.IsServiceRuleChecked = false;
            }
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


        /// <summary>
        /// Adds the application to the exceptions list so that no further notifications will be displayed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnIgnore_Click(object sender, RoutedEventArgs e)
        {
            createRule(false, false);
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
            this.WindowState = WindowState.Minimized;
        }

        public void lblService_Click(object sender, RoutedEventArgs e)
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

        private void btnSkipAll_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void expand_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.Top = ExpectedTop;

            Settings.Default.Save();
        }

        private void btnBlockTemp_Click(object sender, RoutedEventArgs e)
        {
            createRule(false, true);
        }

        private void createRule(bool doAllow, bool isTemp)
        {
            bool success = false;
            var activeConn = ((CurrentConn)lstConnections.SelectedItem);

            string[] services = null;
            if (_optionsView.IsServiceRuleChecked)
            {
                if (activeConn.PossibleServices != null && activeConn.PossibleServices.Length > 0)
                {
                    ServicesForm sf = new ServicesForm(activeConn);
                    sf.ShowDialog();
                    if (sf.DialogResult.Value)
                    {
                        services = sf.CreateAppRule ? null : sf.SelectedServices;
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
                if (_optionsView.IsLocalPortChecked)
                {
                    ((App)Application.Current).Connections.Remove(activeConn);
                }
                else
                {
                    for (int i = ((App)Application.Current).Connections.Count; i > 0; i--)
                    {
                        var c = ((App)Application.Current).Connections[i];
                        if (c.CurrentPath == ((CurrentConn)lstConnections.SelectedItem).CurrentPath)
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
                MessageBox.Show(isTemp ? Common.Resources.MSG_RULE_TMP_FAILED : Common.Resources.MSG_RULE_FAILED, Common.Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool createBlockRule(CurrentConn activeConn, string[] services, bool isTemp)
        {
            bool success = false;
            if (!isTemp)
            {
                if (Settings.Default.UseBlockRules)
                {
                    //Process.Start(new ProcessStartInfo(Application.ExecutablePath, ) { Verb = "runas" });
                    success = FirewallHelper.AddBlockRuleIndirect(activeConn.RuleName, activeConn.CurrentPath, services, _optionsView.IsProtocolChecked ? activeConn.Protocol : -1, _optionsView.IsTargetIPChecked ? activeConn.Target : null, _optionsView.IsTargetPortChecked ? activeConn.TargetPort : null, _optionsView.IsLocalPortChecked ? activeConn.LocalPort : null, _optionsView.IsCurrentProfileChecked);
                    if (!success)
                    {
                        MessageBox.Show(Common.Resources.MSG_RULE_FAILED, Common.Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    string entry = (!_optionsView.IsServiceRuleChecked || String.IsNullOrEmpty(activeConn.CurrentService) ? activeConn.CurrentPath : activeConn.CurrentService) +
                                   (_optionsView.IsLocalPortChecked ? ";" + activeConn.LocalPort : ";") +
                                   (_optionsView.IsTargetIPChecked ? ";" + activeConn.Target : ";") +
                                   (_optionsView.IsTargetPortChecked ? ";" + activeConn.TargetPort : ";");
                    StreamWriter sw = new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exclusions.set"), true);
                    sw.WriteLine(entry);
                    sw.Close();

                    success = true;
                }
            }
            return success;
        }

        private void btnAllowTemp_Click(object sender, RoutedEventArgs e)
        {
            createRule(true, true);
        }

        private bool createAllowRule(CurrentConn activeConn, string[] services, bool isTemp)
        {
            bool success = false;
            if (isTemp)
            {
                success = FirewallHelper.AddTempRuleIndirect(activeConn.RuleName, activeConn.CurrentPath, services, _optionsView.IsProtocolChecked ? activeConn.Protocol : -1, _optionsView.IsTargetIPChecked ? activeConn.Target : null, _optionsView.IsTargetPortChecked ? activeConn.TargetPort : null, _optionsView.IsLocalPortChecked ? activeConn.LocalPort : null, _optionsView.IsCurrentProfileChecked);
            }
            else
            {
                success = FirewallHelper.AddAllowRuleIndirect(activeConn.RuleName, activeConn.CurrentPath, services, _optionsView.IsProtocolChecked ? activeConn.Protocol : -1, _optionsView.IsTargetIPChecked ? activeConn.Target : null, _optionsView.IsTargetPortChecked ? activeConn.TargetPort : null, _optionsView.IsLocalPortChecked ? activeConn.LocalPort : null, _optionsView.IsCurrentProfileChecked);
            }
            return success;
        }

        private void btnSkip_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).Connections.Remove((CurrentConn)lstConnections.SelectedItem);
        }

        private void hlkPath_Navigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start("explorer.exe", "/select," + e.Uri);
        }
    }
}
