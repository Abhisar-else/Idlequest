using IdleQuest.Domain.Aggregates;
using IdleQuest.Domain.Enums;

namespace IdleQuest.Application.Interfaces.Repositories;

public interface IItemRepository
{
    Task<Item?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Item>> GetAllAsync(CancellationToken ct = default);
}
