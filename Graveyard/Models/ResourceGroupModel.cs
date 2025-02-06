using Graveyard.Interfaces;

namespace Graveyard.Models
{
    public class ResourceGroupModel : IGrave
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SubscriptionId { get; set; }

        public List<ResourceModel> Resources { get; set; }
        public TagModel Tags { get; set; }
    }
}
