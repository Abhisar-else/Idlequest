using System.Text.Json;
using IdleQuest.Application.DTOs;
using IdleQuest.Application.Interfaces.Repositories;
using IdleQuest.Application.Interfaces.Services;
using IdleQuest.Domain;
using IdleQuest.Domain.Aggregates;
using IdleQuest.Domain.Enums;
using IdleQuest.Domain.Supporting;
using IdleQuest.Domain.ValueObjects;

namespace IdleQuest.Application.Services;

public sealed class SaveLoadService : ISaveLoadService
{
    private readonly ISaveGameRepository _saves;
    private readonly IPlayerRepository _players;

    public SaveLoadService(ISaveGameRepository saves, IPlayerRepository players)
    {
        _saves   = saves;
        _players = players;
    }

    public async Task<IReadOnlyList<SaveSlotDto>> ListAsync(Guid playerId, CancellationToken ct = default)
    {
        var rows = await _saves.GetAllForPlayerAsync(playerId, ct);
        var dict = rows.ToDictionary(r => r.Slot);
        return Enum.GetValues<SaveSlot>().Select(s =>
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

        // Full snapshot — version 2 includes XP, HP, inventory and quests.
        // Version 1 only stored 5 fields (HeroName, Level, Gold, ZoneId, LastLogin).
        var snapshot = JsonSerializer.Serialize(new PlayerSnapshotV2(
            Version:      2,
            HeroName:     p.HeroName,
            Class:        (int)p.Class,
            Level:        p.Level,
            Experience:   p.Experience,
            ExperienceToNext: p.ExperienceToNext,
            PrestigeLevel: p.PrestigeLevel,
            Gold:         p.Gold,
            CurrentHp:    p.CurrentHp,
            CurrentZoneId: p.CurrentZoneId,
            LastLoginAt:  p.LastLoginAt,
            BaseStats:    new StatBlockSnapshot(
                p.BaseStats.MaxHp, p.BaseStats.Attack, p.BaseStats.Defense,
                p.BaseStats.Speed, p.BaseStats.MagicPower,
                p.BaseStats.CritChance, p.BaseStats.CritMultiplier),
            Inventory:    p.Inventory.Select(i => new InventorySnapshot(
                i.Id, i.Name, (int)i.Slot, (int)i.Rarity, i.Bonus, i.Emoji)).ToList(),
            WeaponId:     p.Equipment.WeaponId,
            ArmorId:      p.Equipment.ArmorId,
            ActiveQuests: p.ActiveQuests.Select(q => new QuestStateSnapshot(
                q.QuestId, q.Progress, q.Completed)).ToList()));

        await _saves.UpsertAsync(new SaveGame
        {
            Id          = Guid.NewGuid(),
            PlayerId    = playerId,
            Slot        = slot,
            Label       = $"{p.HeroName} — Lv.{p.Level}  ({p.Gold}g)",
            Snapshot    = snapshot,
            SavedAt     = DateTime.UtcNow,
            SaveVersion = 2
        }, ct);

        return true;
    }

    public async Task<bool> LoadAsync(Guid playerId, SaveSlot slot, CancellationToken ct = default)
    {
        var row = await _saves.GetAsync(playerId, slot, ct);
        if (row is null) return false;

        var p = await _players.GetByIdAsync(playerId, ct);
        if (p is null) return false;

        // Support loading both snapshot versions
        if (row.SaveVersion >= 2)
        {
            var snap = JsonSerializer.Deserialize<PlayerSnapshotV2>(row.Snapshot);
            if (snap is null) return false;

            p.HeroName          = snap.HeroName;
            p.Class             = (CharacterClass)snap.Class;
            p.Level             = snap.Level;
            p.Experience        = snap.Experience;
            p.ExperienceToNext  = snap.ExperienceToNext;
            p.PrestigeLevel     = snap.PrestigeLevel;
            p.Gold              = snap.Gold;
            p.CurrentZoneId     = snap.CurrentZoneId;
            p.LastLoginAt       = snap.LastLoginAt;
            p.BaseStats         = new StatBlock(
                snap.BaseStats.MaxHp, snap.BaseStats.Attack, snap.BaseStats.Defense,
                snap.BaseStats.Speed, snap.BaseStats.MagicPower,
                snap.BaseStats.CritChance, snap.BaseStats.CritMultiplier);

            p.Inventory = snap.Inventory.Select(i => new InventoryEntry
            {
                Id     = i.Id,
                Name   = i.Name,
                Slot   = (ItemSlot)i.Slot,
                Rarity = (ItemRarity)i.Rarity,
                Bonus  = i.Bonus,
                Emoji  = i.Emoji
            }).ToList();

            p.Equipment = new EquipmentState
            {
                WeaponId = snap.WeaponId,
                ArmorId  = snap.ArmorId
            };

            p.ActiveQuests = snap.ActiveQuests.Select(q => new PlayerQuestState
            {
                QuestId   = q.QuestId,
                Progress  = q.Progress,
                Completed = q.Completed
            }).ToList();

            var eff = p.EffectiveStats();
            p.CurrentHp = Math.Min(snap.CurrentHp > 0 ? snap.CurrentHp : eff.MaxHp, eff.MaxHp);
        }
        else
        {
            // Legacy v1 snapshot — restore the 5 fields that were saved
            var snap = JsonSerializer.Deserialize<PlayerSnapshotV1>(row.Snapshot);
            if (snap is null) return false;

            p.HeroName      = snap.HeroName;
            p.Level         = snap.Level;
            p.Gold          = snap.Gold;
            p.CurrentZoneId = snap.CurrentZoneId;
            p.LastLoginAt   = snap.LastLoginAt;
            var eff = p.EffectiveStats();
            p.CurrentHp = Math.Min(p.CurrentHp, eff.MaxHp);
        }

        await _players.UpdateAsync(p, ct);
        return true;
    }

    // ── Snapshot types ───────────────────────────────────────────────────────

    private sealed record PlayerSnapshotV2(
        int Version,
        string HeroName,
        int Class,
        int Level,
        long Experience,
        long ExperienceToNext,
        int PrestigeLevel,
        long Gold,
        int CurrentHp,
        int CurrentZoneId,
        DateTime LastLoginAt,
        StatBlockSnapshot BaseStats,
        List<InventorySnapshot> Inventory,
        Guid? WeaponId,
        Guid? ArmorId,
        List<QuestStateSnapshot> ActiveQuests);

    private sealed record StatBlockSnapshot(
        int MaxHp, int Attack, int Defense, int Speed, int MagicPower,
        float CritChance, float CritMultiplier);

    private sealed record InventorySnapshot(
        Guid Id, string Name, int Slot, int Rarity, int Bonus, string Emoji);

    private sealed record QuestStateSnapshot(
        Guid QuestId, int Progress, bool Completed);

    // Legacy v1 format (kept for backward compat)
    private sealed record PlayerSnapshotV1(
        string HeroName, int Level, long Gold, int CurrentZoneId, DateTime LastLoginAt);
}