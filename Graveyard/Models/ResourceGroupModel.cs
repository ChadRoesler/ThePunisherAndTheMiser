namespace Graveyard.Models
{
    public class ResourceGroupModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SubscriptionId { get; set; }
        public string Location { get; set; }
        public TagModel Tags { get; set; }
    }
}
