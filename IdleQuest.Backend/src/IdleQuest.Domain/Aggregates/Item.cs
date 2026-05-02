using IdleQuest.Domain.Enums;
using IdleQuest.Domain.ValueObjects;

namespace IdleQuest.Domain.Aggregates;

/// <summary>Catalog / loot template row (not player inventory).</summary>
public class Item
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public ItemSlot Slot { get; set; }
    public ItemRarity Rarity { get; set; }
    public StatBlock StatBonus { get; set; } = StatBlock.Zero;
    public string Emoji { get; set; } = "?";
    public int BuyPrice { get; set; }
}
