using IdleQuest.Domain.Enums;

namespace IdleQuest.Domain.Aggregates;

public class SaveGame
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public SaveSlot Slot { get; set; }
    public string Label { get; set; } = "";
    public string Snapshot { get; set; } = "{}";
    public DateTime SavedAt { get; set; }
    public int SaveVersion { get; set; } = 1;
}
