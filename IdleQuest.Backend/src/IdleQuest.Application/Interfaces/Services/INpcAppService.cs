using IdleQuest.Application.DTOs;

namespace IdleQuest.Application.Interfaces.Services;

public interface INpcAppService
{
    Task<IReadOnlyList<object>> NpcsInZoneAsync(int zoneId, CancellationToken ct = default);
    Task<object?> DialogueAsync(Guid playerId, Guid npcId, CancellationToken ct = default);
    Task<IReadOnlyList<InventoryItemDto>?> ShopAsync(Guid npcId, CancellationToken ct = default);
    Task<PlayerSummaryDto?> BuyAsync(Guid playerId, Guid npcId, Guid itemId, CancellationToken ct = default);
}
