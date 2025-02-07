using Azure.ResourceManager.Resources;

namespace Graveyard.ExtensionMethods
{
    internal static class ResourceGroupDataExtension
    {
        public static Dictionary<string, string> VisibleTags(this ResourceGroupData data)
        {
            var tags = new Dictionary<string, string>();
            if (data.Tags != null && data.Tags.Count > 0)
            {
                var nonHiddenTagKeys = data.Tags.Keys.Where(x => !x.StartsWith("hidden-", StringComparison.OrdinalIgnoreCase));
                foreach (var key in nonHiddenTagKeys)
                {
                    tags[key] = data.Tags[key] ?? string.Empty;
                }
            }
            return tags;
        }
    }
}
