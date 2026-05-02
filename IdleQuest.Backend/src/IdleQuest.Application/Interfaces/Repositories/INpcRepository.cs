using IdleQuest.Domain.Aggregates;

namespace IdleQuest.Application.Interfaces.Repositories;

public interface INpcRepository
{
    Task<List<Npc>> GetByZoneAsync(int zoneId, CancellationToken ct = default);
    Task<Npc?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
