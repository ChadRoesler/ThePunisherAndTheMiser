namespace Graveyard.Models
{
    public class TagModel
    {
        public string ObjectId { get; set; } = string.Empty;
        public string ObjectType { get; set; } = string.Empty;

        public Dictionary<string, string> CurrentTags { get; set; } = [];

        public List<HistoricTagModel> TagHistory { get; set; } = [];
    }
}
