using IdleQuest.Application.DTOs;

namespace IdleQuest.Application.Interfaces.Services;

public interface ICombatService
{
    Task<CombatStateDto> StartAsync(Guid playerId, int zoneId, CancellationToken ct = default);
    Task<CombatStateDto> AttackAsync(Guid sessionId, Guid playerId, CancellationToken ct = default);
    Task<CombatStateDto> FleeAsync(Guid sessionId, Guid playerId, CancellationToken ct = default);
    Task<IdleRewardsDto> ClaimIdleRewardsAsync(Guid playerId, CancellationToken ct = default);
}
