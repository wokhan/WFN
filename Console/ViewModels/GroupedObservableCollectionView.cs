using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Data;

using Wokhan.Collections;

namespace Wokhan.WindowsFirewallNotifier.Console.ViewModels;

/// <summary>
/// Allows to create a <see cref="CollectionView"> from a <see cref="GroupedObservableCollection{TK, T}"/>, hence having a view from already grouped data (hence mimicking the IsGroupSourced from UWP/WinUI, which isn't available in WPF).
/// Note: this would be useless if <see cref="GroupedObservableCollection{TK, T}"/> had an <see cref="ObservableCollection{TItem}"/> property exposing all Values, which we would then use as a source for a standard <see cref="CollectionViewSource" />.
/// </summary>
/// <typeparam name="TKey">Type of the key the collection is grouped by</typeparam>
/// <typeparam name="TItem">Type of each item in the collection groups</typeparam>
public class GroupedObservableCollectionView<TKey, TItem> : ListCollectionView where TKey : class
                                                                               where TItem : class
{
    private readonly ObservableCollection<TItem> typedSourceCollection;

    /// <summary>
    /// Creates a new <see cref="GroupedObservableCollectionView{TKey, TItem}"/> from an already grouped collection, grouping by the property specified by <paramref name="groupByPropertyName"/>.
    /// </summary>
    /// <param name="collection">Source collection to get the view for. This collection must already be grouped by the key retrieved by keyGetter."/></param>
    /// <param name="groupByPropertyName">Property to group on (must be of <see cref="TItem"> type)</param>
    public GroupedObservableCollectionView(GroupedObservableCollection<TKey, TItem> collection, string groupByPropertyName) : base(new ObservableCollection<TItem>(collection.SelectMany(item => item)))
    {
        ArgumentNullException.ThrowIfNull(groupByPropertyName);

        typedSourceCollection = (ObservableCollection<TItem>)SourceCollection;

        this.GroupDescriptions.Add(new PropertyGroupDescription(groupByPropertyName));

        collection.CollectionChanged += SourceCollection_CollectionChanged;
    }

    /// <summary>
    /// Builds a new <see cref="GroupedObservableCollectionView{TKey, TItem}"/> from an already grouped collection, grouping using the <paramref name="keyGetter"/> specifier to access either a composite property or a new object as a key.
    /// </summary>
    /// <param name="collection">Source collection to get the view for. This collection must already be grouped by the key retrieved by keyGetter."/></param>
    /// <param name="keyGetter">Method to retrieve the key to group values on</param>
    public GroupedObservableCollectionView(GroupedObservableCollection<TKey, TItem> collection, Func<TItem, TKey> keyGetter) : base(new ObservableCollection<TItem>(collection.SelectMany(item => item)))
    {
        ArgumentNullException.ThrowIfNull(keyGetter);

        typedSourceCollection = (ObservableCollection<TItem>)SourceCollection;

        this.GroupDescriptions.Add(new GroupedCollectionGroupDescription<TKey, TItem>(keyGetter));

        collection.CollectionChanged += SourceCollection_CollectionChanged;
    }

    private void SourceCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            foreach (var item in e.NewItems!.Cast<ObservableGrouping<TKey, TItem>>())
            {
                item.CollectionChanged += Item_CollectionChanged;
            }
        }
    }

    private void Item_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(sender);

        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            foreach (var it in e.NewItems!)
            {
                typedSourceCollection.Add((TItem)it);
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            foreach (var it in e.OldItems!)
            {
                typedSourceCollection.Remove((TItem)it);
            }
        }
    }
}