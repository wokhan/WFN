using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using Wokhan.WindowsFirewallNotifier.Common.Logging;
using Wokhan.WindowsFirewallNotifier.Common.Net.WFP;
using Wokhan.WindowsFirewallNotifier.Common.Processes;
using Wokhan.WindowsFirewallNotifier.Console.ViewModels;

using WFPRules = Wokhan.WindowsFirewallNotifier.Common.Net.WFP.Rules;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Interaction logic for Rules.xaml
    /// </summary>
    public partial class Rules : Page
    {
        FirewallStatusWrapper status = new FirewallStatusWrapper();

        public bool IsFirewallEnabled => status.PrivateIsEnabled || status.PublicIsEnabled || status.DomainIsEnabled;

        public Rules()
        {
            this.Loaded += Rules_Loaded;

            InitializeComponent();
        }

        private void Rules_Loaded(object sender, RoutedEventArgs e)
        {
            initRules();
            filterRules();
        }

        private List<WFPRules::Rule> allRules;

        private string _filter = String.Empty;
        public string Filter
        {
            get { return _filter; }
            set { _filter = value; filterRules(); }
        }

        public enum TypeFilterEnum
        {
            ALL, ACTIVE, WFN, WSH
        }
        public static Dictionary<TypeFilterEnum, string> TypeFilters => new Dictionary<TypeFilterEnum, string> {
                                                     { TypeFilterEnum.WFN, "WFN rules" },
                                                     { TypeFilterEnum.ACTIVE, "Active rules" },
                                                     { TypeFilterEnum.ALL, "Show all" },
                                                     { TypeFilterEnum.WSH, "WSH rules (Windows hidden rules)" }
                };

        private TypeFilterEnum _typeFilter = TypeFilterEnum.WFN;
        public TypeFilterEnum TypeFilter
        {
            get { return _typeFilter; }
            set { _typeFilter = value; filterRules(); }
        }

        private void initRules()
        {
            LogHelper.Debug("Retrieving all rules...");
            try
            {
                allRules = FirewallHelper.GetRules(AlsoGetInactive: true).ToList();
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to load all FW rules", e);
            }
        }

        private void filterRules()
        {
            LogHelper.Debug("Filtering rules...");
            try
            {
                Predicate<WFPRules::Rule> pred = null;
                switch (TypeFilter)
                {
                    case TypeFilterEnum.ACTIVE:
                        pred += activeRulesPredicate;
                        break;

                    case TypeFilterEnum.WFN:
                        pred += WFNRulesPredicate;
                        break;

                    case TypeFilterEnum.WSH:
                        pred += WSHRulesPredicate;
                        break;

                    case TypeFilterEnum.ALL: 
                    default:
                        break;
                }

                if (Filter.Length > 0)
                {
                    pred += filteredRulesPredicate;  // text filter
                }

                //This code is messy, but the WPF DataGrid forgets the sorting when you change the ItemsSource, and you have to restore it in TWO places.
                System.ComponentModel.SortDescription oldSorting = gridRules.Items.SortDescriptions.FirstOrDefault();
                String oldSortingPropertyName = oldSorting.PropertyName ?? gridRules.Columns.FirstOrDefault().Header.ToString();
                System.ComponentModel.ListSortDirection oldSortingDirection = oldSorting.Direction;
                gridRules.ItemsSource = (pred is null ? allRules : allRules.Where(r => pred.GetInvocationList().All(p => ((Predicate<WFPRules::Rule>)p)(r)))).ToList();
                gridRules.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription(oldSortingPropertyName, oldSortingDirection));
                foreach (var column in gridRules.Columns)
                {
                    if (column.Header.ToString() == oldSortingPropertyName)
                    {
                        column.SortDirection = oldSortingDirection;
                    }
                }
                gridRules.Items.Refresh();
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to filter FW rules", e);
            }
        }

        private static readonly string rulePrefix = Common.Properties.Resources.RULE_NAME_FORMAT.Split('-')[0];
        private static readonly string oldRulePrefix = Common.Properties.Resources.RULE_NAME_FILTER_PREFIX2;
        private static readonly string rulePrefixAlt2 = Common.Properties.Resources.RULE_NAME_FILTER_PREFIX3;
        private static readonly string tempRulePrefix = Common.Properties.Resources.RULE_TEMP_PREFIX;
        private bool WFNRulesPredicate(WFPRules::Rule r)
        {
            return r.Name.StartsWith(rulePrefix, StringComparison.Ordinal) 
                || r.Name.StartsWith(oldRulePrefix, StringComparison.Ordinal) 
                || r.Name.StartsWith(rulePrefixAlt2, StringComparison.Ordinal) 
                || r.Name.StartsWith(tempRulePrefix, StringComparison.Ordinal);
        }

        private bool WSHRulesPredicate(WFPRules::Rule r)
        {
            return r.Name.StartsWith(Common.Properties.Resources.RULE_WSH_PREFIX, StringComparison.Ordinal);
        }

        private bool activeRulesPredicate(WFPRules::Rule r)
        {
            return r.Enabled;
        }

        private bool filteredRulesPredicate(WFPRules::Rule r)
        {
            return (r.Name.IndexOf(txtFilter.Text, StringComparison.OrdinalIgnoreCase) > -1 || (r.ApplicationName != null && r.ApplicationName.IndexOf(txtFilter.Text, StringComparison.CurrentCultureIgnoreCase) > -1));
        }

        private void btnRemoveRule_Click(object sender, RoutedEventArgs e)
        {
            System.Collections.IList selectedRules = gridRules.SelectedItems;
            if (selectedRules is null || selectedRules.Count == 0)
            {
                return;
            }
            if (MessageBox.Show(Common.Properties.Resources.MSG_RULE_DELETE, Common.Properties.Resources.MSG_DLG_TITLE, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                foreach (WFPRules::Rule selectedRule in selectedRules)
                {

                   if (!FirewallHelper.RemoveRule(selectedRule.Name))
                   {
                       MessageBox.Show(Common.Properties.Resources.MSG_RULE_DELETE_FAILED, Common.Properties.Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
                       return;
                   }
                   allRules.Remove(selectedRule);
                }
                filterRules();
            }
        }

        private void btnLocate_Click(object sender, RoutedEventArgs e)
        {
            var selectedRule = (WFPRules::Rule)gridRules.SelectedItem;
            if (selectedRule is null)
            {
                //@
                return;
            }
            ProcessHelper.StartShellExecutable("explorer.exe", "/select," + selectedRule.ApplicationName, true);
        }

        private void btnStartAdvConsole_Click(object sender, RoutedEventArgs e)
        {
            ProcessHelper.StartShellExecutable("WF.msc", null, true);
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            initRules();
            filterRules();
        }
    }
}
