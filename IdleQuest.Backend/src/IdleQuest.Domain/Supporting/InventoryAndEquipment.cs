using IdleQuest.Domain.Enums;

namespace IdleQuest.Domain.Supporting;

public sealed class InventoryEntry
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public ItemSlot Slot { get; set; }
    public ItemRarity Rarity { get; set; }
    public int Bonus { get; set; }
    public string Emoji { get; set; } = "?";
}

public sealed class EquipmentState
{
    public Guid? WeaponId { get; set; }
    public Guid? ArmorId { get; set; }
}

public sealed class PlayerQuestState
{
    public Guid QuestId { get; set; }
    public int Progress { get; set; }
    public bool Completed { get; set; }
}

public sealed class SkillEntry
{
    public string Id { get; set; } = "";
    public int Rank { get; set; }
}

public sealed class CombatLogLine
{
    public DateTime At { get; set; }
    public string Message { get; set; } = "";
    public string Severity { get; set; } = "info";
}
