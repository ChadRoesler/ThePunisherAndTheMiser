using Graveyard.Services;

namespace Graveyard.Models
{
    public class ResourceModel
    {
        public string Name { get; set; }
        public string ResourceType { get; set; }
        public string Id { get; set; }
        public string ResourceGroupId { get; set; }

        public string Location { get; set; }

        public TagModel Tags { get; set; }
        
    }
}
