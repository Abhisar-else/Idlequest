using IdleQuest.Application.DTOs;
using IdleQuest.Application.Interfaces.Repositories;
using IdleQuest.Application.Interfaces.Services;
using IdleQuest.Application.Mapping;
using IdleQuest.Domain;
using IdleQuest.Domain.Supporting;

namespace IdleQuest.Application.Services;

public sealed class NpcAppService : INpcAppService
{
    private readonly INpcRepository _npcs;
    private readonly IItemRepository _items;
    private readonly IPlayerRepository _players;
    private readonly IZoneRepository _zones;

    public NpcAppService(INpcRepository npcs, IItemRepository items, IPlayerRepository players, IZoneRepository zones)
    {
        _npcs = npcs;
        _items = items;
        _players = players;
        _zones = zones;
    }

    public async Task<IReadOnlyList<object>> NpcsInZoneAsync(int zoneId, CancellationToken ct = default)
    {
        var list = await _npcs.GetByZoneAsync(zoneId, ct);
        return list.Select(n => (object)new { n.Id, n.Name, n.IsMerchant }).ToList();
    }

    public Task<object?> DialogueAsync(Guid playerId, Guid npcId, CancellationToken ct = default)
    {
        return Task.FromResult<object?>(new { npcId, lines = Array.Empty<string>(), note = "Visit again soon." });
    }

    public async Task<IReadOnlyList<InventoryItemDto>?> ShopAsync(Guid npcId, CancellationToken ct = default)
    {
        var npc = await _npcs.GetByIdAsync(npcId, ct);
        if (npc is null || !npc.IsMerchant) return null;

        var result = new List<InventoryItemDto>();
        foreach (var row in npc.Shop)
        {
            var item = await _items.GetByIdAsync(row.ItemId, ct);
            if (item is null) continue;
            result.Add(new InventoryItemDto(item.Id, item.Name, item.Slot.ToString(), item.Rarity.ToString(),
                item.StatBonus.Attack + item.StatBonus.Defense + item.StatBonus.MaxHp / 5, item.Emoji));
        }

        return result;
    }

    public async Task<PlayerSummaryDto?> BuyAsync(Guid playerId, Guid npcId, Guid itemId, CancellationToken ct = default)
    {
        var npc = await _npcs.GetByIdAsync(npcId, ct) ?? throw new DomainException("NPC not found.");
        var listing = npc.Shop.FirstOrDefault(s => s.ItemId == itemId)
                      ?? throw new DomainException("Item not sold here.");
        var item = await _items.GetByIdAsync(itemId, ct) ?? throw new DomainException("Unknown item.");

        var price = listing.Price > 0 ? listing.Price : item.BuyPrice;
        var p = await _players.GetByIdAsync(playerId, ct) ?? throw new DomainException("Player not found.");
        if (p.Gold < price) throw new DomainException("Not enough gold.");

        p.Gold -= price;
        p.Inventory.Add(new InventoryEntry
        {
            Id = Guid.NewGuid(),
            Name = item.Name,
            Slot = item.Slot,
            Rarity = item.Rarity,
            Bonus = item.StatBonus.Attack + item.StatBonus.Defense,
            Emoji = item.Emoji
        });

        await _players.UpdateAsync(p, ct);
        var z = await _zones.GetByIdAsync(p.CurrentZoneId, ct);
        return PlayerMapper.ToSummary(p, z?.Name);
    }
}
