using IdleQuest.API.Auth;
using IdleQuest.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdleQuest.API.Controllers;

[Authorize]
[ApiController]
[Route("api/quests")]
public sealed class QuestsController : ControllerBase
{
    private readonly IQuestAppService _quests;

    public QuestsController(IQuestAppService quests) => _quests = quests;

    [HttpGet("available")]
    public async Task<IActionResult> Available(CancellationToken ct) =>
        Ok(await _quests.AvailableAsync(User.GetPlayerId(), ct));

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct)
    {
        var q = await _quests.AcceptAsync(User.GetPlayerId(), id, ct);
        return q is null ? NotFound() : Ok(q);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var p = await _quests.CompleteAsync(User.GetPlayerId(), id, ct);
        return p is null ? NotFound() : Ok(p);
    }
}
