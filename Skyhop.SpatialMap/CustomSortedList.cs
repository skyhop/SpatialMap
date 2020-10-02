using System;
using System.Collections.Generic;
using System.Reflection;

namespace Skyhop.SpatialMap
{
    internal class CustomSortedList<TKey, TValue> : SortedList<TKey, TValue>
        where TKey : notnull
    {
        private readonly FieldInfo _keysField = typeof(CustomSortedList<TKey, TValue>).BaseType.GetField("keys", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly FieldInfo _valuesField = typeof(CustomSortedList<TKey, TValue>).BaseType.GetField("values", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly FieldInfo _comparerField = typeof(CustomSortedList<TKey, TValue>).BaseType.GetField("comparer", BindingFlags.Instance | BindingFlags.NonPublic);

        // Returns the index of the entry with a given key in this sorted list. The
        // key is located through a binary search, and thus the average execution
        // time of this method is proportional to Log2(size), where
        // size is the size of this sorted list. The returned value is -1 if
        // the given key does not occur in this sorted list. Null is an invalid 
        // key value.
        // 
        public int RoughIndexOfKey(TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            int ret = Array.BinarySearch<TKey>(
                (TKey[])_keysField.GetValue(this),
                0,
                Count,
                key,
                (IComparer<TKey>)_comparerField.GetValue(this));

            return ret >= 0 ? ret : ~ret;
        }

        // Returns the value of the entry at the given index.
        // 
        public TValue GetByIndex(int index)
        {
            if (index < 0 || index > Count) return default;
            return ((TValue[])_valuesField.GetValue(this))[index];
        }
    }
}
