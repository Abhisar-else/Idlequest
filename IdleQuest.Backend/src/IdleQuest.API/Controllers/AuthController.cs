using IdleQuest.Application.DTOs;
using IdleQuest.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdleQuest.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<ActionResult<AuthResultDto>> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        var r = await _auth.RegisterAsync(req, ct);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResultDto>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var r = await _auth.LoginAsync(req, ct);
        return r.Success ? Ok(r) : Unauthorized(r);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResultDto>> Refresh([FromBody] RefreshRequest req, CancellationToken ct)
    {
        var r = await _auth.RefreshTokenAsync(req.RefreshToken, ct);
        return r.Success ? Ok(r) : Unauthorized(r);
    }
}
