namespace Graveyard.Models
{
    public class TagModel
    {
        public string ObjectId { get; set; }
        public string ObjectType { get; set; }

        public Dictionary<string, string> CurrentTags { get; set; } = new();

        public List<HistoricTagModel> TagHistory { get; set; } = new();
    }
}
