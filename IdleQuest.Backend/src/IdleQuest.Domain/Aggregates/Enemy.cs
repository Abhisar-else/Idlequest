using IdleQuest.Domain.ValueObjects;

namespace IdleQuest.Domain.Aggregates;

public class LootRow
{
    public Guid ItemId { get; set; }
    public double Weight { get; set; }
}

public class Enemy
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Emoji { get; set; } = "👹";
    public int ZoneId { get; set; }
    public int Level { get; set; }
    public StatBlock Stats { get; set; } = StatBlock.Zero;
    public List<LootRow> LootTable { get; set; } = new();
}
