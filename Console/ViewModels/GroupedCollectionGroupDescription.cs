using System;
using System.ComponentModel;
using System.Globalization;

using Wokhan.Collections;

namespace Wokhan.WindowsFirewallNotifier.Console.ViewModels;

public class GroupedCollectionGroupDescription<TKey, TItem> : GroupDescription
{
    public override object GroupNameFromItem(object item, int level, CultureInfo culture)
    {
        if (item is not ObservableGrouping<TKey, TItem> connectionsGroup)
        {
            throw new ArgumentException($"Unexpected parameter passed: required type of {typeof(ObservableGrouping<TKey, TItem>)}, but parameter 'item' was of type {item?.GetType()}");
        }

        return connectionsGroup.Key!;
    }
}

