using IdleQuest.Application.DTOs;
using IdleQuest.Application.Interfaces.Repositories;
using IdleQuest.Application.Interfaces.Services;
using IdleQuest.Application.Mapping;
using IdleQuest.Domain;
using IdleQuest.Domain.Enums;
using IdleQuest.Domain.Supporting;

namespace IdleQuest.Application.Services;

public sealed class InventoryService : IInventoryService
{
    private readonly IPlayerRepository _players;
    private readonly IZoneRepository _zones;
    private readonly ICacheService _cache;

    public InventoryService(IPlayerRepository players, IZoneRepository zones, ICacheService cache)
    {
        _players = players;
        _zones   = zones;
        _cache   = cache;
    }

    public async Task<(IReadOnlyList<InventoryItemDto> Items, EquippedDto Equipped)?> GetInventoryAsync(
        Guid playerId, CancellationToken ct = default)
    {
        var p = await _players.GetByIdAsync(playerId, ct);
        if (p is null) return null;

        var items = p.Inventory.Select(MapInv).ToList();
        InventoryItemDto? w = null, a = null;
        foreach (var i in p.Inventory)
        {
            if (i.Id == p.Equipment.WeaponId) w = MapInv(i);
            if (i.Id == p.Equipment.ArmorId)  a = MapInv(i);
        }

        return (items, new EquippedDto(w, a));
    }

    public async Task<PlayerSummaryDto?> EquipAsync(Guid playerId, Guid itemId, CancellationToken ct = default)
    {
        var p = await _players.GetByIdAsync(playerId, ct) ?? throw new DomainException("Player not found.");
        p.Equip(itemId);
        await _players.UpdateAsync(p, ct);
        await _cache.RemoveAsync($"player:{playerId}", ct);
        var z = await _zones.GetByIdAsync(p.CurrentZoneId, ct);
        return PlayerMapper.ToSummary(p, z?.Name);
    }

    public async Task<PlayerSummaryDto?> UnequipAsync(Guid playerId, ItemSlot slot, CancellationToken ct = default)
    {
        var p = await _players.GetByIdAsync(playerId, ct) ?? throw new DomainException("Player not found.");
        p.Unequip(slot);
        await _players.UpdateAsync(p, ct);
        await _cache.RemoveAsync($"player:{playerId}", ct);
        var z = await _zones.GetByIdAsync(p.CurrentZoneId, ct);
        return PlayerMapper.ToSummary(p, z?.Name);
    }

    public async Task<(PlayerSummaryDto? Player, long GoldGained)?> SellAsync(
        Guid playerId, Guid itemId, CancellationToken ct = default)
    {
        var p = await _players.GetByIdAsync(playerId, ct) ?? throw new DomainException("Player not found.");
        var gold = p.SellItem(itemId);
        await _players.UpdateAsync(p, ct);
        await _cache.RemoveAsync($"player:{playerId}", ct);
        var z = await _zones.GetByIdAsync(p.CurrentZoneId, ct);
        return (PlayerMapper.ToSummary(p, z?.Name), gold);
    }

    /// <summary>
    /// Uses a consumable item. Health Potion restores HP proportional to its MaxHp stat bonus.
    /// The item is removed from inventory after use.
    /// </summary>
    public async Task<PlayerSummaryDto?> UseAsync(Guid playerId, Guid itemId, CancellationToken ct = default)
    {
        var p = await _players.GetByIdAsync(playerId, ct) ?? throw new DomainException("Player not found.");
        var item = p.FindInventory(itemId) ?? throw new DomainException("Item not found in inventory.");

        if (item.Slot != ItemSlot.Consumable)
            throw new DomainException("Only consumable items can be used.");

        var eff = p.EffectiveStats();

        // item.Bonus stores (Attack + Defense + MaxHp/10) from GrantRealLoot / BuyAsync.
        // Health Potion: StatBonus = (MaxHp:30, Atk:0, Def:0) → Bonus = 0+0+30/10 = 3
        // Restore HP using the raw bonus * 10 as a proxy for the original MaxHp stat,
        // clamped to the player's effective max HP.
        var healAmount = Math.Max(20, item.Bonus * 10);
        p.CurrentHp = Math.Min(eff.MaxHp, p.CurrentHp + healAmount);

        // Remove the consumed item from inventory
        p.Inventory.RemoveAll(i => i.Id == itemId);

        await _players.UpdateAsync(p, ct);
        await _cache.RemoveAsync($"player:{playerId}", ct);

        var z = await _zones.GetByIdAsync(p.CurrentZoneId, ct);
        return PlayerMapper.ToSummary(p, z?.Name);
    }

    private static InventoryItemDto MapInv(InventoryEntry i) =>
        new(i.Id, i.Name, i.Slot.ToString(), i.Rarity.ToString(), i.Bonus, i.Emoji);
}