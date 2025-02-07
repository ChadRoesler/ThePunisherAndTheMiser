namespace Graveyard.ExtensionMethods
{
    public static class DictionaryExtensions
    {
        public static bool IsEqualTo<TKey, TValue>(this Dictionary<TKey, TValue> rootDictionary, Dictionary<TKey, TValue> comparedDictionary, StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase)
        {
            if (rootDictionary is null)
            {
                throw new ArgumentNullException(nameof(rootDictionary));
            }

            if (comparedDictionary is null)
            {
                throw new ArgumentNullException(nameof(comparedDictionary));
            }

            if (rootDictionary.Count != comparedDictionary.Count)
            {
                return false;
            }

            foreach (var kvp in rootDictionary)
            {
                if (!comparedDictionary.TryGetValue(kvp.Key, out var comparedValue) || !EqualityComparer<TValue>.Default.Equals(kvp.Value, comparedValue))
                {
                    return false;
                }
            }
            return true;
        }

        public static void Merge<TKey, TValue>(this Dictionary<TKey, TValue> rootDictionary, Dictionary<TKey, TValue> comparedDictionary, bool updateIfKeyExists = false)
        {
            if (rootDictionary is null)
            {
                throw new ArgumentNullException(nameof(rootDictionary));
            }
            if (comparedDictionary is null)
            {
                throw new ArgumentNullException(nameof(comparedDictionary));
            }

            foreach (var kvp in comparedDictionary)
            {
                if (updateIfKeyExists || !rootDictionary.ContainsKey(kvp.Key))
                {
                    rootDictionary[kvp.Key] = kvp.Value;
                }
            }
        }
    }
}
