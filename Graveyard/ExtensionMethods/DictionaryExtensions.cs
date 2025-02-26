namespace Graveyard.ExtensionMethods
{
    public static class DictionaryExtensions
    {
        public static bool IsEqualTo(this Dictionary<string, string> rootDictionary, Dictionary<string, string> comparedDictionary)
        {
            ArgumentNullException.ThrowIfNull(rootDictionary);
            ArgumentNullException.ThrowIfNull(comparedDictionary);

            if (rootDictionary.Count != comparedDictionary.Count)
            {
                return false;
            }

            foreach (var rootEntry in rootDictionary)
            {
                if (!comparedDictionary.TryGetValue(rootEntry.Key, out var comparedValue) || !string.Equals(rootEntry.Value, comparedValue))
                {
                    return false;
                }
            }
            return true;
        }

        public static void Merge(this Dictionary<string, string> rootDictionary, Dictionary<string, string> comparedDictionary, bool overwriteKeyIfEmpty = false, bool overwriteKeyValue = false)
        {
            if (rootDictionary is null)
            {
                ArgumentNullException.ThrowIfNull(rootDictionary);
            }
            if (comparedDictionary is null)
            {
                ArgumentNullException.ThrowIfNull(comparedDictionary);
            }

            foreach (var comparedEntry in comparedDictionary)
            {
                if (overwriteKeyValue || !rootDictionary.TryGetValue(comparedEntry.Key, out var comparedValue) || (overwriteKeyIfEmpty &&  string.IsNullOrWhiteSpace(comparedValue)))
                {
                    rootDictionary[comparedEntry.Key] = comparedEntry.Value;
                }
            }
        }
    }
}
