using IdleQuest.Application.DTOs;

namespace IdleQuest.Application.Interfaces.Services;

public interface IWorldService
{
    Task<IReadOnlyList<ZoneDto>> GetZonesAsync(Guid playerId, CancellationToken ct = default);
    Task<PlayerSummaryDto?> TravelAsync(Guid playerId, int zoneId, CancellationToken ct = default);
    Task<object> GetWorldStateAsync(CancellationToken ct = default);
}
