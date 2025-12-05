using System.Collections.Generic;

namespace SABI
{
    public static class DictionaryExtensions
    {
        /// Extension method for Dictionary that adds a new key-value pair or updates the value if the key exists.
        /// Return this Dictionary for method chaining.
        /// Arguments: TKey key: The key to add or update. TValue value: The value to set.
        public static Dictionary<TKey, TValue> AddOrUpdate<TKey, TValue>(
            this Dictionary<TKey, TValue> dictionary,
            TKey key,
            TValue value
        )
        {
            if (dictionary.ContainsKey(key))
                dictionary[key] = value;
            else
                dictionary.Add(key, value);

            return dictionary;
        }

        /// Extension method for Dictionary that finds the key by its value.
        /// Returns TKey key found for the given value.
        /// Arguments: TValue value: The value to search for.
        public static TKey GetKeyByValue<TKey, TValue>(
            this Dictionary<TKey, TValue> dictionary,
            TKey value
        )
        {
            TKey key = default;
            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            {
                if (pair.Value.Equals(value))
                {
                    key = pair.Key;
                    break;
                }
            }
            return key;
        }

        /// Extension method for Dictionary that checks if a key exists and its value is not null.
        /// Returns bool indicating if the key exists and value is not null.
        /// Arguments: TKey key: The key to check.
        public static bool ContainsAndNotNull<TKey, TValue>(
            this Dictionary<TKey, TValue> dictionary,
            TKey key
        )
            where TValue : UnityEngine.Object =>
            dictionary.ContainsKey(key) && dictionary[key] != null;
    }
}
