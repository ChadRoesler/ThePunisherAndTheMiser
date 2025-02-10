namespace Graveyard.Models
{
    public class ResourceModel
    {
        public string Name { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string ResourceGroupId { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public TagModel Tags { get; set; } = new();

    }
}
