using IdleQuest.Domain.Aggregates;
using IdleQuest.Domain.Enums;

namespace IdleQuest.Application.Interfaces.Repositories;

public interface ISaveGameRepository
{
    Task<SaveGame?> GetAsync(Guid playerId, SaveSlot slot, CancellationToken ct = default);
    Task<List<SaveGame>> GetAllForPlayerAsync(Guid playerId, CancellationToken ct = default);
    Task UpsertAsync(SaveGame save, CancellationToken ct = default);
}
