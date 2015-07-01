using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Interaction logic for Rules.xaml
    /// </summary>
    public partial class Rules : Page
    {
        public Rules()
        {
            InitializeComponent();

            initAllRules();
            initRules();
        }

        private IOrderedEnumerable<FirewallHelper.Rule> allrules;

        private string _filter = String.Empty;
        public string Filter
        {
            get { return _filter; }
            set { _filter = value; initRules(); }
        }

        public Dictionary<int, string> TypeFilters
        {
            get
            {
                return new Dictionary<int, string> { { 0, "Show all" }, 
                                                       { 1, "Active rules" }, 
                                                       { 2, "WFN rules" }, 
                                                       { 3, "WSH rules (Windows hidden rules)" } };
            }
        }

        private int _typeFilter = 0;
        public int TypeFilter
        {
            get { return _typeFilter; }
            set { _typeFilter = value; initRules(); }
        }

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
                switch (TypeFilter)
                {
                    case 1:
                        pred += activeRulesPredicate;
                        break;

                    case 2:
                        pred += WFNRulesPredicate;
                        break;

                    case 3:
                        pred += WSHRulesPredicate;
                        break;
                 
                    case 0:
                    default:
                        break;
                }

                if (Filter.Length > 0)
                {
                    pred += filteredRulesPredicate;
                }

                gridRules.ItemsSource = (pred == null ? allrules : allrules.Where(r => pred.GetInvocationList().All(p => ((Predicate<FirewallHelper.Rule>)p)(r)))).ToList();
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to filter FW rules", e);
            }
        }

        private static string rulePrefix = Common.Resources.RULE_NAME_FORMAT.Split('-')[0];
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

        private void btnRemoveRule_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Common.Resources.MSG_RULE_DELETE, Common.Resources.MSG_DLG_TITLE, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                FirewallHelper.RemoveRule(((FirewallHelper.Rule)gridRules.SelectedItem).Name);

                initAllRules();
                initRules();
            }
        }

        private void btnLocate_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", "/select," + ((FirewallHelper.Rule)gridRules.SelectedItem).ApplicationName);
        }

        private void btnStartAdvConsole_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("WF.msc");
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            initAllRules();
        }
    }
}
