using Graveyard.Models.Interfaces;

namespace Graveyard.Models
{
    public class ResourceGroupModel : IResourceObject
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SubscriptionId { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public TagModel Tags { get; set; } = new();
    }
}
