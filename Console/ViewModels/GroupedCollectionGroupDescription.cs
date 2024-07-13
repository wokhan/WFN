using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Wokhan.WindowsFirewallNotifier.Console.ViewModels;

/// <summary>
/// Creates a GroupDescription to use along with a CollectionView to use an already grouped collection with a WPF Binding.
/// Internally uses a <see cref="ConditionalWeakTable{TKey, TValue}"/> to attach the key to the object itself and "cache" it (kind of).
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TItem"></typeparam>
public class GroupedCollectionGroupDescription<TKey, TItem> : GroupDescription where TKey : class where TItem : class
{
    private readonly Func<TItem, TKey> keyGetter;

    private ConditionalWeakTable<TItem, TKey> cwt = [];

    public GroupedCollectionGroupDescription(Func<TItem, TKey> keyGetter) : base()
    {
        this.keyGetter = keyGetter;
    }

    public override object GroupNameFromItem(object item, int level, CultureInfo culture)
    {
        if (item is not TItem itm)
        {
            throw new ArgumentException($"Unexpected parameter passed: required type of {typeof(TItem)}, but parameter 'item' was of type {item?.GetType()}");
        }

        return cwt.GetValue(itm, item => keyGetter(itm)!);
    }
}

