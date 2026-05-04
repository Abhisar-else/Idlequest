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
    private readonly IQuestRepository _quests;
    private readonly ICacheService _cache;

    public WorldService(
        IZoneRepository zones,
        IPlayerRepository players,
        IQuestRepository quests,
        ICacheService cache)
    {
        _zones   = zones;
        _players = players;
        _quests  = quests;
        _cache   = cache;
    }

    public async Task<IReadOnlyList<ZoneDto>> GetZonesAsync(Guid playerId, CancellationToken ct = default)
    {
        var player = await _players.GetByIdAsync(playerId, ct) ?? throw new DomainException("Player not found.");
        var zones  = await _zones.GetAllAsync(ct);
        return zones.OrderBy(z => z.SortOrder).Select(z => new ZoneDto(
            z.Id,
            z.Name,
            z.Emoji,
            z.RecommendedLevel,
            Unlocked: player.Level >= z.RecommendedLevel - 3 || z.Id == 1,
            Active:   z.Id == player.CurrentZoneId)).ToList();
    }

    public async Task<PlayerSummaryDto?> TravelAsync(Guid playerId, int zoneId, CancellationToken ct = default)
    {
        var player = await _players.GetByIdAsync(playerId, ct) ?? throw new DomainException("Player not found.");
        var zone   = await _zones.GetByIdAsync(zoneId, ct)     ?? throw new DomainException("Zone not found.");

        if (player.Level < zone.RecommendedLevel - 3 && zoneId > 1)
            throw new DomainException("Zone locked. Level up first.");

        player.CurrentZoneId = zoneId;

        // Advance any active "visit" quest objectives whose description mentions this zone.
        // Seed quest: "Deep Woods" objective Id="visit", Description="Travel to Shadow Forest"
        // We match by checking if the zone name appears in the objective description.
        await AdvanceVisitQuestsAsync(player, zone.Name, ct);

        await _players.UpdateAsync(player, ct);
        await _cache.RemoveAsync($"player:{playerId}", ct);
        return PlayerMapper.ToSummary(player, zone.Name);
    }

    public Task<object> GetWorldStateAsync(CancellationToken ct = default) =>
        Task.FromResult<object>(new
        {
            serverTime = DateTime.UtcNow,
            events     = Array.Empty<WorldEventDto>()
        });

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task AdvanceVisitQuestsAsync(Domain.Aggregates.Player player, string zoneName, CancellationToken ct)
    {
        if (player.ActiveQuests.Count == 0) return;

        foreach (var state in player.ActiveQuests.Where(q => !q.Completed))
        {
            var quest = await _quests.GetByIdAsync(state.QuestId, ct);
            if (quest is null) continue;

            // Only process objectives with Id = "visit"
            var visitObj = quest.Objectives.FirstOrDefault(o => o.Id == "visit");
            if (visitObj is null) continue;

            // Check if this zone matches the objective description
            if (!visitObj.Description.Contains(zoneName, StringComparison.OrdinalIgnoreCase)) continue;

            state.Progress++;
            if (state.Progress >= visitObj.TargetAmount)
                state.Completed = true;
        }
    }
}