using IdleQuest.API.Auth;
using IdleQuest.Application.Interfaces.Services;
using IdleQuest.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdleQuest.API.Controllers;

[Authorize]
[ApiController]
[Route("api/saves")]
public sealed class SavesController : ControllerBase
{
    private readonly ISaveLoadService _saves;

    public SavesController(ISaveLoadService saves) => _saves = saves;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _saves.ListAsync(User.GetPlayerId(), ct));

    [HttpPost("{slot}")]
    public async Task<IActionResult> Save(string slot, CancellationToken ct)
    {
        var s = ParseSlot(slot);
        var ok = await _saves.SaveAsync(User.GetPlayerId(), s, ct);
        return ok ? Ok() : BadRequest();
    }

    [HttpPost("{slot}/load")]
    public async Task<IActionResult> Load(string slot, CancellationToken ct)
    {
        var s = ParseSlot(slot);
        var ok = await _saves.LoadAsync(User.GetPlayerId(), s, ct);
        return ok ? Ok() : NotFound();
    }

    private static SaveSlot ParseSlot(string slot) =>
        Enum.Parse<SaveSlot>(slot, true);
}
