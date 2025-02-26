namespace Graveyard.Models.Interfaces
{
    public interface IResourceObject
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public TagModel Tags { get; set; }
    }
}
