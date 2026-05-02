using IdleQuest.Application.DTOs;
using IdleQuest.Domain.Enums;

namespace IdleQuest.Application.Interfaces.Services;

public interface ISaveLoadService
{
    Task<IReadOnlyList<SaveSlotDto>> ListAsync(Guid playerId, CancellationToken ct = default);
    Task<bool> SaveAsync(Guid playerId, SaveSlot slot, CancellationToken ct = default);
    Task<bool> LoadAsync(Guid playerId, SaveSlot slot, CancellationToken ct = default);
}
