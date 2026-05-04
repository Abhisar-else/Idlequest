using IdleQuest.API.Auth;
using IdleQuest.Application.Interfaces.Services;
using IdleQuest.Domain;
using IdleQuest.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdleQuest.API.Controllers;

[Authorize]
[ApiController]
[Route("api/inventory")]
public sealed class InventoryController : ControllerBase
{
    private readonly IInventoryService _inv;

    public InventoryController(IInventoryService inv) => _inv = inv;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var x = await _inv.GetInventoryAsync(User.GetPlayerId(), ct);
        return x is null ? NotFound() : Ok(new { items = x.Value.Items, equipped = x.Value.Equipped });
    }

    [HttpPost("equip/{itemId:guid}")]
    public async Task<IActionResult> Equip(Guid itemId, CancellationToken ct)
    {
        var p = await _inv.EquipAsync(User.GetPlayerId(), itemId, ct);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpDelete("equip/{slot}")]
    public async Task<IActionResult> Unequip(string slot, CancellationToken ct)
    {
        var s = slot.ToLowerInvariant() switch
        {
            "weapon" => ItemSlot.Weapon,
            "armor"  => ItemSlot.Armor,
            _        => throw new DomainException("Slot must be weapon or armor.")
        };
        var p = await _inv.UnequipAsync(User.GetPlayerId(), s, ct);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpPost("{itemId:guid}/sell")]
    public async Task<IActionResult> Sell(Guid itemId, CancellationToken ct)
    {
        var r = await _inv.SellAsync(User.GetPlayerId(), itemId, ct);
        return r is null ? NotFound() : Ok(new { player = r.Value.Player, goldGained = r.Value.GoldGained });
    }

    /// <summary>
    /// POST /api/inventory/{itemId}/use
    /// Uses a consumable item (e.g. Health Potion). Removes item, applies effect, returns updated player.
    /// </summary>
    [HttpPost("{itemId:guid}/use")]
    public async Task<IActionResult> Use(Guid itemId, CancellationToken ct)
    {
        var p = await _inv.UseAsync(User.GetPlayerId(), itemId, ct);
        return p is null ? NotFound() : Ok(p);
    }
}