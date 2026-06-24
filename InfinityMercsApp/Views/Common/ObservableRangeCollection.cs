using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace InfinityMercsApp.Views.Common;

/// <summary>
/// An <see cref="ObservableCollection{T}"/> that can replace its entire contents while raising a
/// single <see cref="NotifyCollectionChangedAction.Reset"/>. Non-virtualized consumers such as
/// <c>BindableLayout</c> rebuild their item views once on a reset instead of once per incremental
/// add, which avoids per-item layout passes when refilling large lists.
/// </summary>
public sealed class ObservableRangeCollection<T> : ObservableCollection<T>
{
    /// <summary>Clears the collection and adds <paramref name="items"/>, raising one Reset.</summary>
    public void ReplaceRange(IEnumerable<T> items)
    {
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
