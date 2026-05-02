using IdleQuest.API.Auth;
using IdleQuest.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdleQuest.API.Controllers;

[Authorize]
[ApiController]
[Route("api/combat")]
public sealed class CombatController : ControllerBase
{
    private readonly ICombatService _combat;

    public CombatController(ICombatService combat) => _combat = combat;

    [HttpPost("start/{zoneId:int}")]
    public async Task<IActionResult> Start(int zoneId, CancellationToken ct) =>
        Ok(await _combat.StartAsync(User.GetPlayerId(), zoneId, ct));

    [HttpPost("{sessionId:guid}/attack")]
    public async Task<IActionResult> Attack(Guid sessionId, CancellationToken ct) =>
        Ok(await _combat.AttackAsync(sessionId, User.GetPlayerId(), ct));

    [HttpPost("{sessionId:guid}/flee")]
    public async Task<IActionResult> Flee(Guid sessionId, CancellationToken ct) =>
        Ok(await _combat.FleeAsync(sessionId, User.GetPlayerId(), ct));

    [HttpPost("idle-rewards")]
    public async Task<IActionResult> IdleRewards(CancellationToken ct) =>
        Ok(await _combat.ClaimIdleRewardsAsync(User.GetPlayerId(), ct));
}
