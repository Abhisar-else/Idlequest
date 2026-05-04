using IdleQuest.Application.DTOs;
using IdleQuest.Domain.Enums;

namespace IdleQuest.Application.Interfaces.Services;

public interface IInventoryService
{
    Task<(IReadOnlyList<InventoryItemDto> Items, EquippedDto Equipped)?> GetInventoryAsync(Guid playerId, CancellationToken ct = default);
    Task<PlayerSummaryDto?> EquipAsync(Guid playerId, Guid itemId, CancellationToken ct = default);
    Task<PlayerSummaryDto?> UnequipAsync(Guid playerId, ItemSlot slot, CancellationToken ct = default);
    Task<(PlayerSummaryDto? Player, long GoldGained)?> SellAsync(Guid playerId, Guid itemId, CancellationToken ct = default);

    /// <summary>
    /// Use a consumable item (e.g. Health Potion). Applies effect and removes from inventory.
    /// Returns updated player summary, or throws DomainException if not consumable.
    /// </summary>
    Task<PlayerSummaryDto?> UseAsync(Guid playerId, Guid itemId, CancellationToken ct = default);
}