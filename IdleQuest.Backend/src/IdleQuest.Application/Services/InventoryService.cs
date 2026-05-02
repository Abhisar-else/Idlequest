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
        _zones = zones;
        _cache = cache;
    }

    public async Task<(IReadOnlyList<InventoryItemDto> Items, EquippedDto Equipped)?> GetInventoryAsync(Guid playerId, CancellationToken ct = default)
    {
        var p = await _players.GetByIdAsync(playerId, ct);
        if (p is null) return null;

        var items = p.Inventory.Select(MapInv).ToList();
        InventoryItemDto? w = null, a = null;
        foreach (var i in p.Inventory)
        {
            if (i.Id == p.Equipment.WeaponId) w = MapInv(i);
            if (i.Id == p.Equipment.ArmorId) a = MapInv(i);
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

    public async Task<(PlayerSummaryDto? Player, long GoldGained)?> SellAsync(Guid playerId, Guid itemId, CancellationToken ct = default)
    {
        var p = await _players.GetByIdAsync(playerId, ct) ?? throw new DomainException("Player not found.");
        var gold = p.SellItem(itemId);
        await _players.UpdateAsync(p, ct);
        await _cache.RemoveAsync($"player:{playerId}", ct);
        var z = await _zones.GetByIdAsync(p.CurrentZoneId, ct);
        return (PlayerMapper.ToSummary(p, z?.Name), gold);
    }

    private static InventoryItemDto MapInv(InventoryEntry i) =>
        new(i.Id, i.Name, i.Slot.ToString(), i.Rarity.ToString(), i.Bonus, i.Emoji);
}
