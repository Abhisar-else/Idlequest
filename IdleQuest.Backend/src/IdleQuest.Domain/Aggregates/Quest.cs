namespace IdleQuest.Domain.Aggregates;

public class QuestObjective
{
    public string Id { get; set; } = "";
    public string Description { get; set; } = "";
    public int TargetAmount { get; set; }
}

public class Quest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public int RequiredLevel { get; set; }
    public long GoldReward { get; set; }
    public long XpReward { get; set; }
    public Guid? GiverNpcId { get; set; }
    public List<QuestObjective> Objectives { get; set; } = new();
}
