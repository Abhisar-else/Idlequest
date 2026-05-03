using IdleQuest.Application.DTOs;
using IdleQuest.Application.Interfaces.Repositories;
using IdleQuest.Application.Interfaces.Services;
using IdleQuest.Domain;
using IdleQuest.Domain.Aggregates;
using IdleQuest.Domain.Enums;
using IdleQuest.Domain.Supporting;

namespace IdleQuest.Application.Services;

public sealed class CombatService : ICombatService
{
    // FIXED: Random.Shared is thread-safe (replaces static readonly Random Rng = new())
    private static Random Rng => Random.Shared;

    private readonly IPlayerRepository _players;
    private readonly IEnemyRepository _enemies;
    private readonly ICombatSessionRepository _sessions;
    private readonly IZoneRepository _zones;
    private readonly IItemRepository _items;
    private readonly IQuestRepository _quests;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IGameHubNotifier _hub;
    private readonly ICacheService _cache;

    public CombatService(
        IPlayerRepository players,
        IEnemyRepository enemies,
        ICombatSessionRepository sessions,
        IZoneRepository zones,
        IItemRepository items,
        IQuestRepository quests,
        IDomainEventDispatcher dispatcher,
        IGameHubNotifier hub,
        ICacheService cache)
    {
        _players    = players;
        _enemies    = enemies;
        _sessions   = sessions;
        _zones      = zones;
        _items      = items;
        _quests     = quests;
        _dispatcher = dispatcher;
        _hub        = hub;
        _cache      = cache;
    }

    public async Task<CombatStateDto> StartAsync(Guid playerId, int zoneId, CancellationToken ct = default)
    {
        var player = await _players.GetByIdAsync(playerId, ct)
                     ?? throw new DomainException("Player not found.");
        var zone = await _zones.GetByIdAsync(zoneId, ct)
                   ?? throw new DomainException("Unknown zone.");

        if (player.Level < zone.RecommendedLevel - 5 && zone.Id > 1)
            throw new DomainException("You are too weak for this zone.");

        var existing = await _sessions.GetActiveForPlayerAsync(playerId, ct);
        if (existing is not null)
        {
            existing.Result = CombatResult.Fled;
            await _sessions.UpdateAsync(existing, ct);
        }

        var enemy = await _enemies.GetRandomForZoneAsync(zoneId, ct);
        var maxHp = Math.Max(20, enemy.Stats.MaxHp + enemy.Level * 4);
        var atk   = Math.Max(5,  enemy.Stats.Attack + enemy.Level * 2);

        var session = new CombatSession
        {
            Id            = Guid.NewGuid(),
            PlayerId      = playerId,
            EnemyEntityId = enemy.Id,
            EnemyName     = enemy.Name,
            EnemyEmoji    = enemy.Emoji,
            EnemyLevel    = enemy.Level,
            EnemyHp       = maxHp,
            EnemyMaxHp    = maxHp,
            EnemyAttack   = atk,
            ZoneId        = zoneId,
            Result        = CombatResult.Ongoing,
            Log           = new List<CombatLogLine>()
        };
        session.Log.Add(new CombatLogLine
        {
            At       = DateTime.UtcNow,
            Message  = $"Encountered {enemy.Name}",
            Severity = "red"
        });

        await _sessions.AddAsync(session, ct);

        await _hub.NotifyPlayerAsync(playerId, "CombatStarted", new
        {
            sessionId = session.Id,
            enemyName = enemy.Name,
            enemyHp   = session.EnemyHp
        });

        return Map(session, player);
    }

    public async Task<CombatStateDto> AttackAsync(Guid sessionId, Guid playerId, CancellationToken ct = default)
    {
        var session = await _sessions.GetByIdAsync(sessionId, ct)
                      ?? throw new DomainException("Combat session not found.");

        if (session.PlayerId != playerId)
            throw new DomainException("Not your combat.");

        if (session.Result != CombatResult.Ongoing)
        {
            var existing = await _players.GetByIdAsync(playerId, ct);
            return Map(session, existing);
        }

        var player = await _players.GetByIdAsync(playerId, ct)
                     ?? throw new DomainException("Player not found.");
        var eff = player.EffectiveStats();

        // Player attacks enemy
        var dmg = Math.Max(6, (int)(eff.Attack * (0.55 + Rng.NextDouble() * 0.55)));
        session.EnemyHp -= dmg;
        session.Log.Add(new CombatLogLine
        {
            At       = DateTime.UtcNow,
            Message  = $"You hit for {dmg}",
            Severity = "orange"
        });

        if (session.EnemyHp <= 0)
        {
            session.Result  = CombatResult.Victory;
            session.EnemyHp = 0;

            var xp   = 60 + session.EnemyLevel * 12 + Rng.Next(40);
            var gold = 18 + Rng.Next(45) + session.EnemyLevel * 2;

            player.GainExperience(xp);
            player.AddGold(gold);
            player.EnemiesSlain++;

            // FIXED: real loot from item repository
            await GrantRealLoot(player, ct);

            // NEW: advance kill quest objectives
            await AdvanceKillQuests(player, ct);

            await _players.UpdateAsync(player, ct);
            await _dispatcher.DispatchAsync(player.DomainEvents, ct);
            player.ClearEvents();

            await _hub.NotifyPlayerAsync(playerId, "CombatVictory", new
            {
                xpGained   = xp,
                goldGained = gold,
                lootCount  = 1
            });

            session.Log.Add(new CombatLogLine
            {
                At       = DateTime.UtcNow,
                Message  = "Victory!",
                Severity = "emerald"
            });
        }
        else
        {
            // Enemy retaliates
            var enemyDmg = Math.Max(4, session.EnemyAttack - eff.Defense / 3 + Rng.Next(6));
            player.CurrentHp = Math.Max(0, player.CurrentHp - enemyDmg);
            session.Log.Add(new CombatLogLine
            {
                At       = DateTime.UtcNow,
                Message  = $"Enemy hits for {enemyDmg}",
                Severity = "red"
            });

            if (player.CurrentHp <= 0)
            {
                session.Result   = CombatResult.Defeat;
                player.CurrentHp = 0;
                session.Log.Add(new CombatLogLine
                {
                    At       = DateTime.UtcNow,
                    Message  = "You were defeated...",
                    Severity = "red"
                });
            }

            await _players.UpdateAsync(player, ct);
        }

        await _sessions.UpdateAsync(session, ct);
        await _cache.RemoveAsync($"player:{playerId}", ct);

        var dto = Map(session, player);
        await _hub.NotifyPlayerAsync(playerId, "CombatUpdate", dto);
        return dto;
    }

    public async Task<CombatStateDto> FleeAsync(Guid sessionId, Guid playerId, CancellationToken ct = default)
    {
        var session = await _sessions.GetByIdAsync(sessionId, ct)
                      ?? throw new DomainException("Combat session not found.");

        if (session.PlayerId != playerId)
            throw new DomainException("Not your combat.");

        session.Result = CombatResult.Fled;
        session.Log.Add(new CombatLogLine
        {
            At       = DateTime.UtcNow,
            Message  = "You fled.",
            Severity = "amber"
        });

        await _sessions.UpdateAsync(session, ct);
        var player = await _players.GetByIdAsync(playerId, ct);
        return Map(session, player!);
    }

    public async Task<IdleRewardsDto> ClaimIdleRewardsAsync(Guid playerId, CancellationToken ct = default)
    {
        var player  = await _players.GetByIdAsync(playerId, ct)
                      ?? throw new DomainException("Player not found.");
        var now     = DateTime.UtcNow;
        var offline = now - player.LastLoginAt;
        if (offline < TimeSpan.Zero) offline = TimeSpan.Zero;

        var capped = Math.Min(8.0, offline.TotalHours);
        var xp     = (long)(capped * (12 + player.Level * 3));
        var gold   = (long)(capped * (8  + player.Level * 2));

        player.GainExperience(xp);
        player.AddGold(gold);
        player.RecordLogin();

        await _players.UpdateAsync(player, ct);
        await _dispatcher.DispatchAsync(player.DomainEvents, ct);
        player.ClearEvents();
        await _cache.RemoveAsync($"player:{playerId}", ct);

        return new IdleRewardsDto(xp, gold, capped);
    }

    // FIXED: draws loot from the actual seeded Item table (45% chance per victory)
    private async Task GrantRealLoot(Player player, CancellationToken ct)
    {
        if (Rng.NextDouble() > 0.45) return;

        var allItems = await _items.GetAllAsync(ct);
        if (allItems.Count == 0) return;

        var template = allItems[Rng.Next(allItems.Count)];

        player.ItemsFound++;
        player.Inventory.Add(new InventoryEntry
        {
            Id     = Guid.NewGuid(),
            Name   = template.Name,
            Slot   = template.Slot,
            Rarity = template.Rarity,
            Bonus  = template.StatBonus.Attack + template.StatBonus.Defense
                     + template.StatBonus.MaxHp / 10,
            Emoji  = template.Emoji
        });
    }

    // NEW: increments progress on active kill quest objectives
    private async Task AdvanceKillQuests(Player player, CancellationToken ct)
    {
        if (player.ActiveQuests.Count == 0) return;

        foreach (var state in player.ActiveQuests.Where(q => !q.Completed))
        {
            var quest = await _quests.GetByIdAsync(state.QuestId, ct);
            if (quest is null) continue;

            var killObj = quest.Objectives.FirstOrDefault(o => o.Id == "kill");
            if (killObj is null) continue;

            state.Progress++;
            if (state.Progress >= killObj.TargetAmount)
                state.Completed = true;
        }
    }

    private static CombatStateDto Map(CombatSession s, Domain.Aggregates.Player? p)
    {
        var eff = p?.EffectiveStats();
        var log = s.Log
                    .OrderByDescending(l => l.At)
                    .Take(12)
                    .Select(l => l.Message)
                    .ToList();

        return new CombatStateDto(
            s.Id,
            s.EnemyEntityId,
            s.EnemyName,
            s.EnemyEmoji,
            s.EnemyLevel,
            s.EnemyHp,
            s.EnemyMaxHp,
            p?.CurrentHp ?? 0,
            eff?.MaxHp   ?? 0,
            s.Result,
            log);
    }
}
