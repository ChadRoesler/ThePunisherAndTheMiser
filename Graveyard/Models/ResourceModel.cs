using Graveyard.Interfaces;


namespace Graveyard.Models
{
    public class ResourceModel: IGrave
    {
        public string Name { get; set; }
        public string ResourceType { get; set; }
        public string Id { get; set; }
        public string ResourceGroupId { get; set; }

        public string Location { get; set; }

        internal TagModel Tags { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
    }
}
