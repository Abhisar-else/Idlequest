using IdleQuest.Domain.Aggregates;

namespace IdleQuest.Application.Interfaces.Repositories;

public interface ICombatSessionRepository
{
    Task<CombatSession?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CombatSession?> GetActiveForPlayerAsync(Guid playerId, CancellationToken ct = default);
    Task AddAsync(CombatSession session, CancellationToken ct = default);
    Task UpdateAsync(CombatSession session, CancellationToken ct = default);
}
