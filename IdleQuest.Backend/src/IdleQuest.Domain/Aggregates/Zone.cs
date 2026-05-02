namespace IdleQuest.Domain.Aggregates;

public class Zone
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Emoji { get; set; } = "🌲";
    public int RecommendedLevel { get; set; }
    public int SortOrder { get; set; }
}
