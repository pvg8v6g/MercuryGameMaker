using System.Collections.ObjectModel;
using GameLibrary.Models;

namespace GameLibrary.Utilities.ComponentModels;

public static class CollectionExtensions
{
    public static ObservableCollectionSorter<T> OrderBy<T, TKey>(this ObservableCollection<T> collection, Func<T, TKey> keySelector)
    {
        return new ObservableCollectionSorter<T>(collection).OrderBy(keySelector);
    }

    public static ObservableCollectionSorter<T> OrderByDescending<T, TKey>(this ObservableCollection<T> collection, Func<T, TKey> keySelector)
    {
        return new ObservableCollectionSorter<T>(collection).OrderByDescending(keySelector);
    }

    public static int AddSorted<T, TKey>(this ObservableCollection<T> collection, T item, Func<T, TKey> keySelector, bool descending = false)
    {
        var comparer = Comparer<TKey>.Default;
        var index = 0;
        while (index < collection.Count)
        {
            var comparison = comparer.Compare(keySelector(collection[index]), keySelector(item));
            if (descending)
            {
                if (comparison < 0) break;
            }
            else
            {
                if (comparison > 0) break;
            }
            index++;
        }
        collection.Insert(index, item);
        return index;
    }
}

public class ObservableCollectionSorter<T>(ObservableCollection<T> collection)
{
    private IOrderedEnumerable<T>? _orderedEnumerable;

    public ObservableCollectionSorter<T> OrderBy<TKey>(Func<T, TKey> keySelector)
    {
        _orderedEnumerable = Enumerable.OrderBy(collection, keySelector);
        return this;
    }

    public ObservableCollectionSorter<T> OrderByDescending<TKey>(Func<T, TKey> keySelector)
    {
        _orderedEnumerable = Enumerable.OrderByDescending(collection, keySelector);
        return this;
    }

    public ObservableCollectionSorter<T> ThenBy<TKey>(Func<T, TKey> keySelector)
    {
        if (_orderedEnumerable == null)
        {
            return OrderBy(keySelector);
        }
        _orderedEnumerable = _orderedEnumerable.ThenBy(keySelector);
        return this;
    }

    public ObservableCollectionSorter<T> ThenByDescending<TKey>(Func<T, TKey> keySelector)
    {
        if (_orderedEnumerable == null)
        {
            return OrderByDescending(keySelector);
        }
        _orderedEnumerable = _orderedEnumerable.ThenByDescending(keySelector);
        return this;
    }

    public void Apply()
    {
        if (_orderedEnumerable == null) return;

        var sorted = _orderedEnumerable.ToList();
        for (var i = 0; i < sorted.Count; i++)
        {
            var oldIndex = collection.IndexOf(sorted[i]);
            if (oldIndex != i)
            {
                collection.Move(oldIndex, i);
            }
        }
    }
}
