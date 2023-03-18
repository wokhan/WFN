using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using Wokhan.WindowsFirewallNotifier.Common.Logging;
using Wokhan.WindowsFirewallNotifier.Common.Net.WFP;
using Wokhan.WindowsFirewallNotifier.Common.Processes;
using Wokhan.WindowsFirewallNotifier.Console.ViewModels;

using WFPRules = Wokhan.WindowsFirewallNotifier.Common.Net.WFP.Rules;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages;


[ObservableObject]
public partial class Rules : Page
{
    FirewallStatusWrapper status = new FirewallStatusWrapper();

    public bool IsFirewallEnabled => status.PrivateIsEnabled || status.PublicIsEnabled || status.DomainIsEnabled;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LocateCommand), nameof(RemoveRuleCommand))]
    private WFPRules::Rule? selectedItem;


    [ObservableProperty]
    private List<WFPRules::Rule> allRules;

    [ObservableProperty]
    private string _textFilter = String.Empty;
    partial void OnTextFilterChanged(string _) => filterRules();

    [ObservableProperty]
    private TypeFilterEnum _typeFilter = TypeFilterEnum.ACTIVE;
    partial void OnTypeFilterChanged(TypeFilterEnum _) => filterRules();

    public Rules()
    {
        this.Loaded += Rules_Loaded;

        InitializeComponent();

        gridRules.Items.Filter = filteredRulesPredicate;
    }

    private void Rules_Loaded(object sender, RoutedEventArgs e)
    {
        initRules();
        filterRules();
    }

    public enum TypeFilterEnum
    {
        ALL, ACTIVE, WFN, WSH
    }
    public static Dictionary<TypeFilterEnum, string> TypeFilters => new()
    {
        { TypeFilterEnum.WFN, "WFN rules" },
        { TypeFilterEnum.ACTIVE, "Active rules" },
        { TypeFilterEnum.ALL, "Show all" },
        { TypeFilterEnum.WSH, "WSH rules (Windows hidden rules)" }
    };

    private void initRules()
    {
        LogHelper.Debug("Retrieving all rules...");
        try
        {
            AllRules = FirewallHelper.GetRules(AlsoGetInactive: true).ToList();
        }
        catch (Exception e)
        {
            LogHelper.Error("Unable to load all FW rules", e);
        }
    }

    Predicate<object>? predType = null;
    private void filterRules()
    {
        gridRules.Items.Filter -= predType;
        gridRules.Items.Filter -= filteredRulesPredicate;

        LogHelper.Debug("Filtering rules...");
        try
        {
            switch (TypeFilter)
            {
                case TypeFilterEnum.ACTIVE:
                    gridRules.Items.Filter += predType = activeRulesPredicate;
                    break;

                case TypeFilterEnum.WFN:
                    gridRules.Items.Filter += predType = WFNRulesPredicate;
                    break;

                case TypeFilterEnum.WSH:
                    gridRules.Items.Filter += predType = WSHRulesPredicate;
                    break;

                case TypeFilterEnum.ALL:
                default:
                    break;
            }

            // We want the text filter to be applied *after* the rule type filter for performance reason, hence the Predicate being added again.
            if (!String.IsNullOrEmpty(TextFilter))
            {
                gridRules.Items.Filter += filteredRulesPredicate;
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
    private bool WFNRulesPredicate(object ruleAsObject)
    {
        var rule = (WFPRules::Rule)ruleAsObject;

        return rule.Name.StartsWith(rulePrefix, StringComparison.Ordinal)
            || rule.Name.StartsWith(oldRulePrefix, StringComparison.Ordinal)
            || rule.Name.StartsWith(rulePrefixAlt2, StringComparison.Ordinal)
            || rule.Name.StartsWith(tempRulePrefix, StringComparison.Ordinal);
    }

    private bool WSHRulesPredicate(object ruleAsObject)
    {
        return ((WFPRules::Rule)ruleAsObject).Name.StartsWith(Common.Properties.Resources.RULE_WSH_PREFIX, StringComparison.Ordinal);
    }

    private bool activeRulesPredicate(object ruleAsObject)
    {
        return ((WFPRules::Rule)ruleAsObject).Enabled;
    }

    private bool filteredRulesPredicate(object ruleAsObject)
    {
        var rule = (WFPRules::Rule)ruleAsObject;
        return (rule.Name.IndexOf(TextFilter, StringComparison.OrdinalIgnoreCase) > -1 || (rule.ApplicationName?.IndexOf(TextFilter, StringComparison.CurrentCultureIgnoreCase) > -1));
    }

    [RelayCommand(CanExecute = nameof(RemoveRuleCanExecute))]
    private void RemoveRule()
    {
        IList selectedRules = gridRules.SelectedItems;
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
                    continue;
                }
                AllRules.Remove(selectedRule);
            }
            filterRules();
        }
    }

    public bool RemoveRuleCanExecute => SelectedItem is not null;

    [RelayCommand(CanExecute = nameof(LocateCanExecute))]
    private void Locate()
    {
        ProcessHelper.BrowseToFile(SelectedItem!.ApplicationName);
    }

    public bool LocateCanExecute => SelectedItem is not null;


    [RelayCommand]
    private void StartAdvConsole()
    {
        ProcessHelper.StartShellExecutable("WF.msc", showMessageBox: true);
    }

    [RelayCommand]
    private void Refresh()
    {
        initRules();
        filterRules();
    }
}
