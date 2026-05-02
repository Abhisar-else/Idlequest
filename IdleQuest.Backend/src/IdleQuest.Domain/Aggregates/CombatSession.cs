using IdleQuest.Domain.Enums;
using IdleQuest.Domain.Supporting;

namespace IdleQuest.Domain.Aggregates;

public class CombatSession
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Guid EnemyEntityId { get; set; }
    public string EnemyName { get; set; } = "";
    public string EnemyEmoji { get; set; } = "";
    public int EnemyLevel { get; set; }
    public int EnemyHp { get; set; }
    public int EnemyMaxHp { get; set; }
    public int EnemyAttack { get; set; }
    public int ZoneId { get; set; }
    public CombatResult Result { get; set; } = CombatResult.Ongoing;
    public List<CombatLogLine> Log { get; set; } = new();
}
