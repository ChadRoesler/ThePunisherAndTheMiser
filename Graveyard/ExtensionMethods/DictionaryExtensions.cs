namespace Graveyard.ExtensionMethods
{
    public static class DictionaryExtensions
    {
        public static bool IsEqualTo<TKey, TValue>(this Dictionary<TKey, TValue> rootDictionary, Dictionary<TKey, TValue> comparedDictionary) where TKey : notnull
        {
            ArgumentNullException.ThrowIfNull(rootDictionary);
            ArgumentNullException.ThrowIfNull(comparedDictionary);

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

        public static void Merge<TKey, TValue>(this Dictionary<TKey, TValue> rootDictionary, Dictionary<TKey, TValue> comparedDictionary, bool updateIfKeyExists = false) where TKey : notnull
        {
            if (rootDictionary is null)
            {
                ArgumentNullException.ThrowIfNull(rootDictionary);
            }
            if (comparedDictionary is null)
            {
                ArgumentNullException.ThrowIfNull(comparedDictionary);
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
