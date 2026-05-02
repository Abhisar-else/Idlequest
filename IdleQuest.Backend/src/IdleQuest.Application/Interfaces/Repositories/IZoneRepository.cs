using IdleQuest.Domain.Aggregates;

namespace IdleQuest.Application.Interfaces.Repositories;

public interface IZoneRepository
{
    Task<List<Zone>> GetAllAsync(CancellationToken ct = default);
    Task<Zone?> GetByIdAsync(int id, CancellationToken ct = default);
}
