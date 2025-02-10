namespace Graveyard.Models
{
    public class HistoricTagModel
    {
        public int Id { get; set; }
        public required Dictionary<string, string> Tags { get; set; }
    }
}
