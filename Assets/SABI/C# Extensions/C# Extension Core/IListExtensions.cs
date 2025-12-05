using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace SABI
{
    public static class IListExtensions
    {
        /// Extension method for IList that checks if the list is null or empty.
        /// Returns bool indicating if the list is null or has no elements.
        public static bool IsNullOrEmpty<T>(this IList<T> list) => list == null || list.Count == 0;

        /// Extension method for IList that gets a random item.
        /// Returns T item randomly selected from the list.
        public static T GetRandomItem<T>(this IList<T> _array)
        {
            if (_array == null)
                throw new System.IndexOutOfRangeException(
                    " [ array is null ] Cannot select a random item from null"
                );
            if (_array.Count == 0)
                throw new System.IndexOutOfRangeException(
                    " [ array is empty ] Cannot select a random item from an empty array"
                );
            return _array[Random.Range(0, _array.Count)];
        }

        /// Extension method for IList that gets unique random items.
        /// Returns List<T> of random items without duplicates.
        /// Arguments: int count: Number of items to select.
        public static List<T> GetUniqueRandomItems<T>(this IList<T> list, int count)
        {
            if (count > list.Count)
                throw new System.ArgumentException("Requested count is larger than list size");

            List<T> result = new List<T>();
            List<T> tempList = new List<T>(list);
            for (int i = 0; i < count; i++)
            {
                int index = Random.Range(0, tempList.Count);
                result.Add(tempList[index]);
                tempList.RemoveAt(index);
            }
            return result;
        }

        /// Extension method for IList that gets unique random items.
        /// Returns List<T> of random items without duplicates with the size of 0 to the specified count.
        /// Arguments: int count: Number of items to select.
        public static List<T> GetUniqueRandomItemsUpTo<T>(this IList<T> list, int count)
        {
            count = System.Math.Min(count, list.Count);

            List<T> result = new List<T>(count);
            List<T> tempList = new List<T>(list);

            for (int i = 0; i < count; i++)
            {
                int index = Random.Range(0, tempList.Count);
                result.Add(tempList[index]);
                tempList.RemoveAt(index);
            }
            return result;
        }

        /// Extension method for List that removes all null elements.
        /// Return this IList<T> for method chaining.
        public static IList<T> RemoveNulls<T>(this List<T> list)
            where T : class
        {
            list.RemoveAll(item => item == null);
            return list;
        }

        /// Extension method for IList that shuffles the elements randomly.
        /// Return this IList<T> for method chaining.
        public static IList<T> Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            return list;
        }

        /// Extension method for IList that gets a random item based on weights.
        /// Returns T item selected using weights.
        /// Arguments: IList<float> weights: List of weights for each item.
        public static T GetWeightedRandom<T>(this IList<T> list, IList<float> weights)
        {
            float totalWeight = 0;
            foreach (float weight in weights)
                totalWeight += weight;

            float random = UnityEngine.Random.Range(0, totalWeight);
            float current = 0;

            for (int i = 0; i < list.Count; i++)
            {
                current += weights[i];
                if (random <= current)
                    return list[i];
            }

            return list[list.Count - 1];
        }

        /// Extension method for IList that executes an action for each item with index.
        /// Return this IList<T> for method chaining.
        /// Arguments: Action<T, int> action: Action to perform on each item and index.
        public static IList<T> ForEach<T>(this IList<T> list, System.Action<T, int> action)
        {
            for (int i = 0; i < list.Count; i++)
                action?.Invoke(list[i], i);
            return list;
        }

        /// Extension method for IList that executes an action for each item.
        /// Return this IList<T> for method chaining.
        /// Arguments: Action<T> action: Action to perform on each item.
        public static IList<T> ForEach<T>(this IList<T> list, System.Action<T> action)
        {
            for (int i = 0; i < list.Count; i++)
                action?.Invoke(list[i]);
            return list;
        }

        /// Extension method for IList that moves an item from one index to another.
        /// Return this IList<T> for method chaining.
        /// Arguments: int oldIndex: Source index. int newIndex: Destination index.
        public static IList<T> Move<T>(this IList<T> list, int oldIndex, int newIndex)
        {
            if (oldIndex == newIndex)
                return list;

            T item = list[oldIndex];
            list.RemoveAt(oldIndex);
            list.Insert(newIndex, item);
            return list;
        }

        /// Extension method for IList that swaps two items.
        /// Return this IList<T> for method chaining.
        /// Arguments: int index1: First index. int index2: Second index.
        public static IList<T> Swap<T>(this IList<T> list, int index1, int index2)
        {
            if (index1 == index2)
                return list;
            ;

            T temp = list[index1];
            list[index1] = list[index2];
            list[index2] = temp;
            return list;
        }

        /// Extension method for IList that replaces the first occurrence of an item.
        /// Return this IList<T> for method chaining.
        /// Arguments: T oldItem: Item to replace. T newItem: New item.
        public static IList<T> Replace<T>(this IList<T> list, T oldItem, T newItem)
        {
            int index = list.IndexOf(oldItem);
            list[index] = newItem;
            return list;
        }

        /// Extension method for IList that removes duplicate items.
        /// Return this IList<T> for method chaining.
        public static IList<T> RemoveDuplicates<T>(this IList<T> list)
        {
            HashSet<T> seen = new HashSet<T>();
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!seen.Add(list[i]))
                    list.RemoveAt(i);
            }
            return list;
        }

        /// Extension method for IList that pops an element by index.
        /// Returns T element removed from the list.
        /// Arguments: int? index: Index to pop, defaults to last.
        public static T Pop<T>(this IList<T> list, int? index = null)
        {
            if (index == null)
                index = list.Count - 1;
            var element = list[index.Value];
            list.RemoveAt(index.Value);

            return element;
        }

        /// Extension method for IList that pops elements by indexes.
        /// Returns List<T> of elements removed from the list.
        /// Arguments: int[] indexes: Indexes to pop.
        public static List<T> PopList<T>(this IList<T> list, params int[] indexes)
        {
            var popped = new List<T>();

            foreach (var index in indexes)
                popped.Add(list.Pop(index));

            return popped;
        }

        /// Extension method for IList that pops a random element.
        /// Returns T element removed randomly from the list.
        public static T PopRandom<T>(this IList<T> list)
        {
            var index = UnityEngine.Random.Range(0, list.Count);
            return list.Pop(index);
        }

        /// Extension method for IList that pops a random element and returns its index.
        /// Returns (T element, int index) tuple of removed element and its index.
        public static (T element, int index) PopRandomTuple<T>(this IList<T> list)
        {
            var index = UnityEngine.Random.Range(0, list.Count);
            return (list.Pop(index), index);
        }

        /// Extension method for IList that pops multiple random elements.
        /// Returns List<T> of elements removed randomly from the list.
        /// Arguments: int count: Number of elements to pop.
        public static List<T> PopRandoms<T>(this IList<T> list, int count)
        {
            var popped = new List<T>();

            for (int i = 0; i < count; i++)
                popped.Add(list.PopRandom());

            return popped;
        }

        /// Extension method for IList that pops multiple random elements and returns their indexes.
        /// Returns List<(T element, int index)> of removed elements and their indexes.
        /// Arguments: int count: Number of elements to pop.
        public static List<(T element, int index)> PopRandomsTupleList<T>(
            this IList<T> list,
            int count
        )
        {
            var popped = new List<(T element, int index)>();

            for (int i = 0; i < count; i++)
                popped.Add(list.PopRandomTuple());

            return popped;
        }

        /// Extension method for IList that removes elements from a starting index to the end.
        /// Return this IList<T> for method chaining.
        /// Arguments: int index: Starting index to remove from.
        public static IList<T> RemoveRange<T>(this IList<T> list, int index)
        {
            for (int i = list.Count - 1; i >= index; i++)
                list.RemoveAt(i);
            return list;
        }

        /// Extension method for IList that adds multiple items.
        /// Return this IList<T> for method chaining.
        /// Arguments: T[] items: Items to add.
        public static IList<T> AddMultiple<T>(this IList<T> list, params T[] items)
        {
            items.ForEach(item => list.Add(item));
            return list;
        }

        /// Extension method for IList that removes multiple items.
        /// Return this IList<T> for method chaining.
        /// Arguments: T[] items: Items to remove.
        public static IList<T> RemoveMultiple<T>(this IList<T> list, params T[] items)
        {
            items.ForEach(item => list.Remove(item));
            return list;
        }

        /// Extension method for IList that clears and adds multiple items.
        /// Return this IList<T> for method chaining.
        /// Arguments: T[] items: Items to set.
        // public static IList<T> SetMultiple<T>(this IList<T> list, params T[] items)
        // {
        //     if (list is T[] array)
        //     {
        //         list = new T[items.Length];

        //         for (int i = 0; i < array.Length; i++)
        //             array[i] = items[i];
        //     }
        //     else
        //     {
        //         if (list == null)
        //             list = new List<T>();
        //         else
        //             list.Clear();
        //         items.ForEach(item => list.Add(item));
        //     }
        //     return list;
        // }
    }
}
