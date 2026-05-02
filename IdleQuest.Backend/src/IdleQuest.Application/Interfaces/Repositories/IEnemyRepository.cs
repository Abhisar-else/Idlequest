using IdleQuest.Domain.Aggregates;

namespace IdleQuest.Application.Interfaces.Repositories;

public interface IEnemyRepository
{
    Task<Enemy?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Enemy>> GetByZoneAsync(int zoneId, CancellationToken ct = default);
    Task<Enemy> GetRandomForZoneAsync(int zoneId, CancellationToken ct = default);
}
