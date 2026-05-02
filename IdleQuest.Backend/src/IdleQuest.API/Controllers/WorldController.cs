using IdleQuest.API.Auth;
using IdleQuest.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdleQuest.API.Controllers;

[Authorize]
[ApiController]
[Route("api/world")]
public sealed class WorldController : ControllerBase
{
    private readonly IWorldService _world;

    public WorldController(IWorldService world) => _world = world;

    [HttpGet("zones")]
    public async Task<IActionResult> Zones(CancellationToken ct) =>
        Ok(await _world.GetZonesAsync(User.GetPlayerId(), ct));

    [HttpPost("zones/{zoneId:int}/travel")]
    public async Task<IActionResult> Travel(int zoneId, CancellationToken ct)
    {
        var p = await _world.TravelAsync(User.GetPlayerId(), zoneId, ct);
        return p is null ? NotFound() : Ok(p);
    }
}
