using IdleQuest.Domain.Aggregates;

namespace IdleQuest.Application.Interfaces.Repositories;

public interface IQuestRepository
{
    Task<Quest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Quest>> GetAvailableForLevelAsync(int level, CancellationToken ct = default);
}
