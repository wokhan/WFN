using System;
using System.Collections.Generic;
using System.Data;
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

            initRules();
            filterRules();
        }

        private List<FirewallHelper.Rule> allRules;

        private string _filter = String.Empty;
        public string Filter
        {
            get { return _filter; }
            set { _filter = value; filterRules(); }
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
            set { _typeFilter = value; filterRules(); }
        }

        private void initRules()
        {
            LogHelper.Debug("Retrieving all rules...");
            try
            {
                allRules = FirewallHelper.GetRules().ToList();
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

                //This code is messy, but the WPF DataGrid forgets the sorting when you change the ItemsSource, and you have to restore it in TWO places.
                System.ComponentModel.SortDescription oldSorting = gridRules.Items.SortDescriptions.FirstOrDefault();
                String oldSortingPropertyName = oldSorting.PropertyName ?? gridRules.Columns.FirstOrDefault().Header.ToString();
                System.ComponentModel.ListSortDirection oldSortingDirection = oldSorting.Direction;
                gridRules.ItemsSource = (pred == null ? allRules : allRules.Where(r => pred.GetInvocationList().All(p => ((Predicate<FirewallHelper.Rule>)p)(r)))).ToList();
                // FIXME: Causes: System.InvalidOperationException: Failed to compare two elements in the array.
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
        private static readonly string tempRuleSuffix = Common.Properties.Resources.RULE_TEMP;
        private bool WFNRulesPredicate(FirewallHelper.Rule r)
        {
            // TODO: Needs testing again
            return r.Name.StartsWith(rulePrefix) || r.Name.StartsWith("[WFN ") || r.Name.EndsWith(tempRuleSuffix);
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
            if (MessageBox.Show(Common.Properties.Resources.MSG_RULE_DELETE, Common.Properties.Resources.MSG_DLG_TITLE, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                FirewallHelper.Rule selectedRule = (FirewallHelper.Rule)gridRules.SelectedItem;
                if (!FirewallHelper.RemoveRule(selectedRule.Name))
                {
                    MessageBox.Show(Common.Properties.Resources.MSG_RULE_DELETE_FAILED, Common.Properties.Resources.MSG_DLG_ERR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                allRules.Remove(selectedRule);

                filterRules();
            }
        }

        private void btnLocate_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", "/select," + ((FirewallHelper.Rule)gridRules.SelectedItem).ApplicationName); //FIXME: Error is SelectedItem is null!
        }

        private void btnStartAdvConsole_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("WF.msc");
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            initRules();
            filterRules();
        }
    }
}
