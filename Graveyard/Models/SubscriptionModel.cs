using Graveyard.Interfaces;

namespace Graveyard.Models
{
    public class SubscriptionModel : IGrave
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public List<ResourceGroupModel> ResourceGroups { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}
