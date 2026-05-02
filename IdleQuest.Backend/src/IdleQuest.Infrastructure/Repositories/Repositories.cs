using IdleQuest.Application.Interfaces.Repositories;
using IdleQuest.Domain;
using IdleQuest.Domain.Aggregates;
using IdleQuest.Domain.Enums;
using IdleQuest.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IdleQuest.Infrastructure.Repositories;

public sealed class PlayerRepository : IPlayerRepository
{
    private readonly IdleQuestDbContext _db;
    public PlayerRepository(IdleQuestDbContext db) => _db = db;

    public Task<Player?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Players.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Player?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        _db.Players.FirstOrDefaultAsync(p => p.Username == username, ct);

    public Task<List<Player>> GetTopPlayersAsync(int count, CancellationToken ct = default) =>
        _db.Players.AsNoTracking().OrderByDescending(p => p.Level).ThenByDescending(p => p.PrestigeLevel)
            .Take(count).ToListAsync(ct);

    public async Task AddAsync(Player player, CancellationToken ct = default)
    {
        _db.Players.Add(player);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Player player, CancellationToken ct = default)
    {
        _db.Players.Update(player);
        await _db.SaveChangesAsync(ct);
    }

    public Task<bool> ExistsAsync(string username, CancellationToken ct = default) =>
        _db.Players.AnyAsync(p => p.Username == username, ct);
}

public sealed class ItemRepository : IItemRepository
{
    private readonly IdleQuestDbContext _db;
    public ItemRepository(IdleQuestDbContext db) => _db = db;

    public Task<Item?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<List<Item>> GetAllAsync(CancellationToken ct = default) =>
        _db.Items.AsNoTracking().ToListAsync(ct);
}

public sealed class EnemyRepository : IEnemyRepository
{
    private readonly IdleQuestDbContext _db;
    private static readonly Random Rng = new();

    public EnemyRepository(IdleQuestDbContext db) => _db = db;

    public Task<Enemy?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Enemies.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<List<Enemy>> GetByZoneAsync(int zoneId, CancellationToken ct = default) =>
        _db.Enemies.AsNoTracking().Where(e => e.ZoneId == zoneId).ToListAsync(ct);

    public async Task<Enemy> GetRandomForZoneAsync(int zoneId, CancellationToken ct = default)
    {
        var enemies = await GetByZoneAsync(zoneId, ct);
        if (enemies.Count == 0) throw new DomainException($"No enemies in zone {zoneId}.");
        return enemies[Rng.Next(enemies.Count)];
    }
}

public sealed class QuestRepository : IQuestRepository
{
    private readonly IdleQuestDbContext _db;
    public QuestRepository(IdleQuestDbContext db) => _db = db;

    public Task<Quest?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Quests.AsNoTracking().FirstOrDefaultAsync(q => q.Id == id, ct);

    public Task<List<Quest>> GetAvailableForLevelAsync(int level, CancellationToken ct = default) =>
        _db.Quests.AsNoTracking().Where(q => q.RequiredLevel <= level).ToListAsync(ct);
}

public sealed class CombatSessionRepository : ICombatSessionRepository
{
    private readonly IdleQuestDbContext _db;
    public CombatSessionRepository(IdleQuestDbContext db) => _db = db;

    public Task<CombatSession?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.CombatSessions.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<CombatSession?> GetActiveForPlayerAsync(Guid playerId, CancellationToken ct = default) =>
        _db.CombatSessions.FirstOrDefaultAsync(
            c => c.PlayerId == playerId && c.Result == CombatResult.Ongoing, ct);

    public async Task AddAsync(CombatSession session, CancellationToken ct = default)
    {
        _db.CombatSessions.Add(session);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(CombatSession session, CancellationToken ct = default)
    {
        _db.CombatSessions.Update(session);
        await _db.SaveChangesAsync(ct);
    }
}

public sealed class SaveGameRepository : ISaveGameRepository
{
    private readonly IdleQuestDbContext _db;
    public SaveGameRepository(IdleQuestDbContext db) => _db = db;

    public Task<SaveGame?> GetAsync(Guid playerId, SaveSlot slot, CancellationToken ct = default) =>
        _db.SaveGames.FirstOrDefaultAsync(s => s.PlayerId == playerId && s.Slot == slot, ct);

    public Task<List<SaveGame>> GetAllForPlayerAsync(Guid playerId, CancellationToken ct = default) =>
        _db.SaveGames.Where(s => s.PlayerId == playerId).ToListAsync(ct);

    public async Task UpsertAsync(SaveGame save, CancellationToken ct = default)
    {
        var existing = await GetAsync(save.PlayerId, save.Slot, ct);
        if (existing is null)
        {
            save.Id = Guid.NewGuid();
            _db.SaveGames.Add(save);
        }
        else
        {
            existing.Snapshot = save.Snapshot;
            existing.Label = save.Label;
            existing.SavedAt = save.SavedAt;
            existing.SaveVersion = save.SaveVersion;
            _db.SaveGames.Update(existing);
        }

        await _db.SaveChangesAsync(ct);
    }
}

public sealed class ZoneRepository : IZoneRepository
{
    private readonly IdleQuestDbContext _db;
    public ZoneRepository(IdleQuestDbContext db) => _db = db;

    public Task<List<Zone>> GetAllAsync(CancellationToken ct = default) =>
        _db.Zones.AsNoTracking().OrderBy(z => z.SortOrder).ToListAsync(ct);

    public Task<Zone?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Zones.AsNoTracking().FirstOrDefaultAsync(z => z.Id == id, ct);
}

public sealed class NpcRepository : INpcRepository
{
    private readonly IdleQuestDbContext _db;
    public NpcRepository(IdleQuestDbContext db) => _db = db;

    public Task<List<Npc>> GetByZoneAsync(int zoneId, CancellationToken ct = default) =>
        _db.Npcs.AsNoTracking().Where(n => n.ZoneId == zoneId).ToListAsync(ct);

    public Task<Npc?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Npcs.FirstOrDefaultAsync(n => n.Id == id, ct);
}
