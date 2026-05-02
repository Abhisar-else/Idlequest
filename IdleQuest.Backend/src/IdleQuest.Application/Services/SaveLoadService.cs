using System.Text.Json;
using IdleQuest.Application.DTOs;
using IdleQuest.Application.Interfaces.Repositories;
using IdleQuest.Application.Interfaces.Services;
using IdleQuest.Domain;
using IdleQuest.Domain.Aggregates;
using IdleQuest.Domain.Enums;

namespace IdleQuest.Application.Services;

public sealed class SaveLoadService : ISaveLoadService
{
    private readonly ISaveGameRepository _saves;
    private readonly IPlayerRepository _players;

    public SaveLoadService(ISaveGameRepository saves, IPlayerRepository players)
    {
        _saves = saves;
        _players = players;
    }

    public async Task<IReadOnlyList<SaveSlotDto>> ListAsync(Guid playerId, CancellationToken ct = default)
    {
        var rows = await _saves.GetAllForPlayerAsync(playerId, ct);
        var dict = rows.ToDictionary(r => r.Slot);
        var slots = Enum.GetValues<SaveSlot>();
        return slots.Select(s =>
        {
            if (!dict.TryGetValue(s, out var row))
                return new SaveSlotDto(s, s.ToString(), null, 0);
            return new SaveSlotDto(row.Slot, row.Label, row.SavedAt, row.SaveVersion);
        }).ToList();
    }

    public async Task<bool> SaveAsync(Guid playerId, SaveSlot slot, CancellationToken ct = default)
    {
        var p = await _players.GetByIdAsync(playerId, ct);
        if (p is null) return false;

        var snapshot = JsonSerializer.Serialize(new PlayerSnapshot(
            p.HeroName,
            p.Level,
            p.Gold,
            p.CurrentZoneId,
            p.LastLoginAt));

        await _saves.UpsertAsync(new SaveGame
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            Slot = slot,
            Label = $"{p.HeroName} — Lv.{p.Level}",
            Snapshot = snapshot,
            SavedAt = DateTime.UtcNow,
            SaveVersion = 1
        }, ct);

        return true;
    }

    public async Task<bool> LoadAsync(Guid playerId, SaveSlot slot, CancellationToken ct = default)
    {
        var row = await _saves.GetAsync(playerId, slot, ct);
        if (row is null) return false;

        var p = await _players.GetByIdAsync(playerId, ct);
        if (p is null) return false;

        var snap = JsonSerializer.Deserialize<PlayerSnapshot>(row.Snapshot);
        if (snap is null) return false;

        p.HeroName = snap.HeroName;
        p.Level = snap.Level;
        p.Gold = snap.Gold;
        p.CurrentZoneId = snap.CurrentZoneId;
        p.LastLoginAt = snap.LastLoginAt;
        var eff = p.EffectiveStats();
        p.CurrentHp = Math.Min(p.CurrentHp, eff.MaxHp);

        await _players.UpdateAsync(p, ct);
        return true;
    }

    private sealed record PlayerSnapshot(string HeroName, int Level, long Gold, int CurrentZoneId, DateTime LastLoginAt);
}
