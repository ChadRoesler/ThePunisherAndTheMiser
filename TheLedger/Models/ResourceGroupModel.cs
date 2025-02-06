namespace TheLedger.Models;

public class ResourceGroupModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public Dictionary<string, string>? Tags { get; set; }
}
