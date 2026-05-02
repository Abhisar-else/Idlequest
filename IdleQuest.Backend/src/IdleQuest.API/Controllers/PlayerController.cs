using IdleQuest.API.Auth;
using IdleQuest.Application.DTOs;
using IdleQuest.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdleQuest.API.Controllers;

[Authorize]
[ApiController]
[Route("api/player")]
public sealed class PlayerController : ControllerBase
{
    private readonly IPlayerAppService _players;

    public PlayerController(IPlayerAppService players) => _players = players;

    [HttpGet("me")]
    public async Task<ActionResult<PlayerSummaryDto>> Me(CancellationToken ct)
    {
        var id = User.GetPlayerId();
        var p = await _players.GetMeAsync(id, ct);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpPatch("me/rename")]
    public async Task<ActionResult<PlayerSummaryDto>> Rename([FromBody] RenameRequest req, CancellationToken ct)
    {
        var p = await _players.RenameAsync(User.GetPlayerId(), req, ct);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpPatch("me/class")]
    public async Task<ActionResult<PlayerSummaryDto>> ChangeClass([FromBody] ChangeClassRequest req, CancellationToken ct)
    {
        var p = await _players.ChangeClassAsync(User.GetPlayerId(), req, ct);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpPost("me/prestige")]
    public async Task<ActionResult<PlayerSummaryDto>> Prestige(CancellationToken ct)
    {
        var p = await _players.PrestigeAsync(User.GetPlayerId(), ct);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpGet("leaderboard")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<LeaderboardEntryDto>>> Leaderboard(CancellationToken ct) =>
        Ok(await _players.LeaderboardAsync(ct));

}
