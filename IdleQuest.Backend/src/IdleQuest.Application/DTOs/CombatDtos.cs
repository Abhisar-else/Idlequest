using IdleQuest.Domain.Enums;

namespace IdleQuest.Application.DTOs;

public sealed record CombatStateDto(
    Guid SessionId,
    Guid EnemyId,
    string EnemyName,
    string EnemyEmoji,
    int EnemyLevel,
    int EnemyHp,
    int EnemyMaxHp,
    int PlayerHp,
    int PlayerMaxHp,
    CombatResult Result,
    IReadOnlyList<string> RecentLog);

public sealed record IdleRewardsDto(long XpGained, long GoldGained, double OfflineHoursCapped);
