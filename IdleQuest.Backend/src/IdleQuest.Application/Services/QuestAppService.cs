using IdleQuest.Application.DTOs;
using IdleQuest.Application.Interfaces.Repositories;
using IdleQuest.Application.Interfaces.Services;
using IdleQuest.Application.Mapping;
using IdleQuest.Domain;
using IdleQuest.Domain.Aggregates;
using IdleQuest.Domain.Supporting;

namespace IdleQuest.Application.Services;

public sealed class QuestAppService : IQuestAppService
{
    private readonly IQuestRepository _quests;
    private readonly IPlayerRepository _players;
    private readonly IZoneRepository _zones;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly ICacheService _cache;

    public QuestAppService(
        IQuestRepository quests,
        IPlayerRepository players,
        IZoneRepository zones,
        IDomainEventDispatcher dispatcher,
        ICacheService cache)
    {
        _quests = quests;
        _players = players;
        _zones = zones;
        _dispatcher = dispatcher;
        _cache = cache;
    }

    public async Task<IReadOnlyList<QuestDto>> AvailableAsync(Guid playerId, CancellationToken ct = default)
    {
        var p = await _players.GetByIdAsync(playerId, ct) ?? throw new DomainException("Player not found.");
        var qs = await _quests.GetAvailableForLevelAsync(p.Level, ct);
        var active = p.ActiveQuests.Select(q => q.QuestId).ToHashSet();
        return qs.Where(q => !active.Contains(q.Id)).Select(MapQuest).ToList();
    }

    public async Task<QuestDto?> AcceptAsync(Guid playerId, Guid questId, CancellationToken ct = default)
    {
        var p = await _players.GetByIdAsync(playerId, ct) ?? throw new DomainException("Player not found.");
        var q = await _quests.GetByIdAsync(questId, ct) ?? throw new DomainException("Quest not found.");
        if (p.Level < q.RequiredLevel) throw new DomainException("Level too low.");
        if (p.ActiveQuests.Any(x => x.QuestId == questId)) throw new DomainException("Already accepted.");

        p.ActiveQuests.Add(new PlayerQuestState { QuestId = questId, Progress = 0, Completed = false });
        await _players.UpdateAsync(p, ct);
        await _cache.RemoveAsync($"player:{playerId}", ct);
        return MapQuest(q);
    }

    public async Task<PlayerSummaryDto?> CompleteAsync(Guid playerId, Guid questId, CancellationToken ct = default)
    {
        var p = await _players.GetByIdAsync(playerId, ct) ?? throw new DomainException("Player not found.");
        var state = p.ActiveQuests.FirstOrDefault(x => x.QuestId == questId)
                    ?? throw new DomainException("Quest not active.");
        var q = await _quests.GetByIdAsync(questId, ct) ?? throw new DomainException("Quest not found.");

        state.Progress = q.Objectives.Sum(o => o.TargetAmount);
        state.Completed = true;
        p.ActiveQuests.RemoveAll(x => x.QuestId == questId);
        p.GainExperience(q.XpReward);
        p.AddGold(q.GoldReward);

        await _players.UpdateAsync(p, ct);
        await _dispatcher.DispatchAsync(p.DomainEvents, ct);
        p.ClearEvents();
        await _cache.RemoveAsync($"player:{playerId}", ct);

        var z = await _zones.GetByIdAsync(p.CurrentZoneId, ct);
        return PlayerMapper.ToSummary(p, z?.Name);
    }

    private static QuestDto MapQuest(Quest q) =>
        new(q.Id, q.Title, q.RequiredLevel, q.GoldReward, q.XpReward,
            q.Objectives.Select(o => o.Description).ToList());
}
