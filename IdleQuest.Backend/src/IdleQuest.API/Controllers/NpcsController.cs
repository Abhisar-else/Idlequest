using IdleQuest.API.Auth;
using IdleQuest.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdleQuest.API.Controllers;

[Authorize]
[ApiController]
[Route("api/npcs")]
public sealed class NpcsController : ControllerBase
{
    private readonly INpcAppService _npcs;

    public NpcsController(INpcAppService npcs) => _npcs = npcs;

    [HttpGet("zone/{zoneId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> ByZone(int zoneId, CancellationToken ct) =>
        Ok(await _npcs.NpcsInZoneAsync(zoneId, ct));

    [HttpPost("{id:guid}/dialogue")]
    public async Task<IActionResult> Dialogue(Guid id, CancellationToken ct) =>
        Ok(await _npcs.DialogueAsync(User.GetPlayerId(), id, ct));

    [HttpGet("{id:guid}/shop")]
    public async Task<IActionResult> Shop(Guid id, CancellationToken ct)
    {
        var shop = await _npcs.ShopAsync(id, ct);
        return shop is null ? NotFound() : Ok(shop);
    }

    [HttpPost("{npcId:guid}/shop/{itemId:guid}")]
    public async Task<IActionResult> Buy(Guid npcId, Guid itemId, CancellationToken ct)
    {
        var p = await _npcs.BuyAsync(User.GetPlayerId(), npcId, itemId, ct);
        return p is null ? NotFound() : Ok(p);
    }
}
