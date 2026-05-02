using IdleQuest.Application.DTOs;
using IdleQuest.Application.Interfaces.Repositories;
using IdleQuest.Application.Interfaces.Services;
using IdleQuest.Application.Mapping;
using IdleQuest.Domain;
using IdleQuest.Domain.Enums;

namespace IdleQuest.Application.Services;

public sealed class PlayerAppService : IPlayerAppService
{
    private readonly IPlayerRepository _players;
    private readonly IZoneRepository _zones;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly ICacheService _cache;

    public PlayerAppService(
        IPlayerRepository players,
        IZoneRepository zones,
        IDomainEventDispatcher dispatcher,
        ICacheService cache)
    {
        _players = players;
        _zones = zones;
        _dispatcher = dispatcher;
        _cache = cache;
    }

    public async Task<PlayerSummaryDto?> GetMeAsync(Guid playerId, CancellationToken ct = default)
    {
        var key = $"player:{playerId}";
        var cached = await _cache.GetAsync<PlayerSummaryDto>(key, ct);
        if (cached is not null) return cached;

        var p = await _players.GetByIdAsync(playerId, ct);
        if (p is null) return null;
        var zone = await _zones.GetByIdAsync(p.CurrentZoneId, ct);
        var dto = PlayerMapper.ToSummary(p, zone?.Name);
        await _cache.SetAsync(key, dto, TimeSpan.FromMinutes(5), ct);
        return dto;
    }

    public async Task<PlayerSummaryDto?> RenameAsync(Guid playerId, RenameRequest req, CancellationToken ct = default)
    {
        var p = await _players.GetByIdAsync(playerId, ct) ?? throw new DomainException("Player not found.");
        p.Rename(req.Name);
        await _players.UpdateAsync(p, ct);
        await Invalidate(playerId, ct);
        return await GetMeAsync(playerId, ct);
    }

    public async Task<PlayerSummaryDto?> ChangeClassAsync(Guid playerId, ChangeClassRequest req, CancellationToken ct = default)
    {
        var p = await _players.GetByIdAsync(playerId, ct) ?? throw new DomainException("Player not found.");
        var cls = Enum.Parse<CharacterClass>(req.Class, true);
        p.ChangeClass(cls);
        await _players.UpdateAsync(p, ct);
        await _dispatcher.DispatchAsync(p.DomainEvents, ct);
        p.ClearEvents();
        await Invalidate(playerId, ct);
        return await GetMeAsync(playerId, ct);
    }

    public async Task<PlayerSummaryDto?> PrestigeAsync(Guid playerId, CancellationToken ct = default)
    {
        var p = await _players.GetByIdAsync(playerId, ct) ?? throw new DomainException("Player not found.");
        p.Prestige();
        await _players.UpdateAsync(p, ct);
        await _dispatcher.DispatchAsync(p.DomainEvents, ct);
        p.ClearEvents();
        await Invalidate(playerId, ct);
        await _cache.RemoveAsync("leaderboard:top", ct);
        return await GetMeAsync(playerId, ct);
    }

    public async Task<IReadOnlyList<LeaderboardEntryDto>> LeaderboardAsync(CancellationToken ct = default)
    {
        const string key = "leaderboard:top";
        var cached = await _cache.GetAsync<List<LeaderboardEntryDto>>(key, ct);
        if (cached is not null) return cached;

        var top = await _players.GetTopPlayersAsync(20, ct);
        var list = top.Select(p => new LeaderboardEntryDto(p.HeroName, p.Level, p.PrestigeLevel, p.Gold)).ToList();
        await _cache.SetAsync(key, list, TimeSpan.FromMinutes(2), ct);
        return list;
    }

    private async Task Invalidate(Guid playerId, CancellationToken ct)
    {
        await _cache.RemoveAsync($"player:{playerId}", ct);
        await _cache.RemoveAsync("leaderboard:top", ct);
    }
}
