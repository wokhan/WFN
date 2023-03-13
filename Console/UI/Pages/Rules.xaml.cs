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
    private WFPRules::Rule selectedItem;

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

    [ObservableProperty]
    private List<WFPRules::Rule> allRules;

    [ObservableProperty]
    private string _filter = String.Empty;
    partial void OnFilterChanged(string value) => filterRules();

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


    [ObservableProperty]
    private TypeFilterEnum _typeFilter = TypeFilterEnum.ACTIVE;

    partial void OnTypeFilterChanged(TypeFilterEnum value) => filterRules();

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

    private void filterRules()
    {
        LogHelper.Debug("Filtering rules...");
        try
        {
            Predicate<WFPRules::Rule>? predType = null;
            switch (TypeFilter)
            {
                case TypeFilterEnum.ACTIVE:
                    predType = activeRulesPredicate;
                    break;

                case TypeFilterEnum.WFN:
                    predType = WFNRulesPredicate;
                    break;

                case TypeFilterEnum.WSH:
                    predType = WSHRulesPredicate;
                    break;

                case TypeFilterEnum.ALL:
                default:
                    break;
            }

            Predicate<WFPRules::Rule>? predText = null;
            if (Filter.Length > 0)
            {
                predText = filteredRulesPredicate;  // text filter
            }

            if (predText is not null || predType is not null)
            {
                gridRules.Items.Filter = item => (predText?.Invoke((WFPRules::Rule)item) ?? true) && (predType?.Invoke((WFPRules::Rule)item) ?? true);
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
    private bool WFNRulesPredicate(WFPRules::Rule rule)
    {
        return rule.Name.StartsWith(rulePrefix, StringComparison.Ordinal)
            || rule.Name.StartsWith(oldRulePrefix, StringComparison.Ordinal)
            || rule.Name.StartsWith(rulePrefixAlt2, StringComparison.Ordinal)
            || rule.Name.StartsWith(tempRulePrefix, StringComparison.Ordinal);
    }

    private bool WSHRulesPredicate(WFPRules::Rule rule)
    {
        return rule.Name.StartsWith(Common.Properties.Resources.RULE_WSH_PREFIX, StringComparison.Ordinal);
    }

    private bool activeRulesPredicate(WFPRules::Rule rule)
    {
        return rule.Enabled;
    }

    private bool filteredRulesPredicate(WFPRules::Rule rule)
    {
        return (rule.Name.IndexOf(Filter, StringComparison.OrdinalIgnoreCase) > -1 || (rule.ApplicationName is not null && rule.ApplicationName.IndexOf(Filter, StringComparison.CurrentCultureIgnoreCase) > -1));
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
                allRules.Remove(selectedRule);
            }
            filterRules();
        }
    }

    public bool RemoveRuleCanExecute => SelectedItem is not null;

    [RelayCommand(CanExecute = nameof(LocateCanExecute))]
    private void Locate()
    {
        ProcessHelper.BrowseToFile(SelectedItem.ApplicationName);
    }

    public bool LocateCanExecute => SelectedItem is not null;


    [RelayCommand]
    private void StartAdvConsole()
    {
        ProcessHelper.StartShellExecutable("WF.msc", null, true);
    }

    [RelayCommand]
    private void Refresh()
    {
        initRules();
        filterRules();
    }
}
