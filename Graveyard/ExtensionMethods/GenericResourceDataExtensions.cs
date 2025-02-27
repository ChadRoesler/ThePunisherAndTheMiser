﻿using Azure.ResourceManager.Resources;
using Graveyard.Constants;

namespace Graveyard.ExtensionMethods
{
    public static class GenericResourceDataExtensions
    {
        public static Dictionary<string, string> VisibleTags(this GenericResourceData data)
        {
            var tags = new Dictionary<string, string>();
            if (data.Tags != null && data.Tags.Count > 0)
            {
                var nonHiddenTagKeys = data.Tags.Keys.Where(x => !x.StartsWith(ResourceStrings.HiddenTagKeyStartsWith, StringComparison.OrdinalIgnoreCase));
                foreach (var key in nonHiddenTagKeys)
                {
                    tags[key] = data.Tags[key] ?? string.Empty;
                }
            }
            return tags;
        }
    }
}
