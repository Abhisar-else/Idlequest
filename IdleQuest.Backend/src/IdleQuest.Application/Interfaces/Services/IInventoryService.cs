using IdleQuest.Application.DTOs;
using IdleQuest.Domain.Enums;

namespace IdleQuest.Application.Interfaces.Services;

public interface IInventoryService
{
    Task<(IReadOnlyList<InventoryItemDto> Items, EquippedDto Equipped)?> GetInventoryAsync(Guid playerId, CancellationToken ct = default);
    Task<PlayerSummaryDto?> EquipAsync(Guid playerId, Guid itemId, CancellationToken ct = default);
    Task<PlayerSummaryDto?> UnequipAsync(Guid playerId, ItemSlot slot, CancellationToken ct = default);
    Task<(PlayerSummaryDto? Player, long GoldGained)?> SellAsync(Guid playerId, Guid itemId, CancellationToken ct = default);
}
