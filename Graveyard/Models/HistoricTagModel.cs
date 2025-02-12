namespace Graveyard.Models
{
    public class HistoricTagModel
    {
        public int Id { get; set; }
        public required Dictionary<string, string> Tags { get; set; }
        public required DateTimeOffset? Timestamp { get; set; }
        public required string ObjectId { get; set; }
        public required string ObjectType { get; set; }
    }
}
