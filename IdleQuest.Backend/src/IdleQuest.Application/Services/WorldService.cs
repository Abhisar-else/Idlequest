using IdleQuest.Application.DTOs;
using IdleQuest.Application.Interfaces.Repositories;
using IdleQuest.Application.Interfaces.Services;
using IdleQuest.Application.Mapping;
using IdleQuest.Domain;

namespace IdleQuest.Application.Services;

public sealed class WorldService : IWorldService
{
    private readonly IZoneRepository _zones;
    private readonly IPlayerRepository _players;
    private readonly ICacheService _cache;

    public WorldService(IZoneRepository zones, IPlayerRepository players, ICacheService cache)
    {
        _zones = zones;
        _players = players;
        _cache = cache;
    }

    public async Task<IReadOnlyList<ZoneDto>> GetZonesAsync(Guid playerId, CancellationToken ct = default)
    {
        var player = await _players.GetByIdAsync(playerId, ct) ?? throw new DomainException("Player not found.");
        var zones = await _zones.GetAllAsync(ct);
        return zones.OrderBy(z => z.SortOrder).Select(z => new ZoneDto(
            z.Id,
            z.Name,
            z.Emoji,
            z.RecommendedLevel,
            Unlocked: player.Level >= z.RecommendedLevel - 3 || z.Id == 1,
            Active: z.Id == player.CurrentZoneId)).ToList();
    }

    public async Task<PlayerSummaryDto?> TravelAsync(Guid playerId, int zoneId, CancellationToken ct = default)
    {
        var player = await _players.GetByIdAsync(playerId, ct) ?? throw new DomainException("Player not found.");
        var zone = await _zones.GetByIdAsync(zoneId, ct) ?? throw new DomainException("Zone not found.");
        if (player.Level < zone.RecommendedLevel - 3 && zoneId > 1)
            throw new DomainException("Zone locked. Level up first.");
        player.CurrentZoneId = zoneId;
        await _players.UpdateAsync(player, ct);
        await _cache.RemoveAsync($"player:{playerId}", ct);
        return PlayerMapper.ToSummary(player, zone.Name);
    }

    public Task<object> GetWorldStateAsync(CancellationToken ct = default)
    {
        return Task.FromResult<object>(new
        {
            serverTime = DateTime.UtcNow,
            events = Array.Empty<WorldEventDto>()
        });
    }
}
