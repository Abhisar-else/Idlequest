using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IdleQuest.Application.DTOs;
using IdleQuest.Application.Interfaces.Repositories;
using IdleQuest.Application.Interfaces.Services;
using IdleQuest.Application.Mapping;
using IdleQuest.Domain.Aggregates;
using IdleQuest.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace IdleQuest.Infrastructure.Auth;

public sealed class JwtAuthService : IAuthService
{
    private readonly IPlayerRepository _players;
    private readonly IConfiguration _config;
    private readonly IDomainEventDispatcher _dispatcher;

    private static readonly Dictionary<string, (Guid PlayerId, DateTime Expires)> RefreshTokens = new();

    public JwtAuthService(IPlayerRepository players, IConfiguration config, IDomainEventDispatcher dispatcher)
    {
        _players = players;
        _config = config;
        _dispatcher = dispatcher;
    }

    public async Task<AuthResultDto> RegisterAsync(RegisterRequest req, CancellationToken ct = default)
    {
        if (await _players.ExistsAsync(req.Username, ct))
            return Fail("Username already taken.");

        var cls = Enum.Parse<CharacterClass>(req.Class, true);
        var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);
        var player = Player.Create(Guid.NewGuid(), req.Username, hash, req.HeroName, cls);

        await _players.AddAsync(player, ct);
        await _dispatcher.DispatchAsync(player.DomainEvents, ct);
        player.ClearEvents();

        return IssueTokens(player);
    }

    public async Task<AuthResultDto> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var player = await _players.GetByUsernameAsync(req.Username, ct);
        if (player is null || !BCrypt.Net.BCrypt.Verify(req.Password, player.PasswordHash))
            return Fail("Invalid credentials.");

        player.RecordLogin();
        await _players.UpdateAsync(player, ct);
        await _dispatcher.DispatchAsync(player.DomainEvents, ct);
        player.ClearEvents();

        return IssueTokens(player);
    }

    public async Task<AuthResultDto> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        if (!RefreshTokens.TryGetValue(refreshToken, out var entry) || entry.Expires < DateTime.UtcNow)
            return Fail("Refresh token expired or invalid.");

        var player = await _players.GetByIdAsync(entry.PlayerId, ct);
        if (player is null) return Fail("Player not found.");

        RefreshTokens.Remove(refreshToken);
        return IssueTokens(player);
    }

    private AuthResultDto IssueTokens(Player player)
    {
        var secret = _config["Jwt:SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey missing.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(8);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, player.Id.ToString()),
            new Claim("playerId", player.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, player.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            _config["Jwt:Issuer"],
            _config["Jwt:Audience"],
            claims,
            expires: expires,
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        RefreshTokens[refresh] = (player.Id, DateTime.UtcNow.AddDays(30));

        return new AuthResultDto(true, jwt, refresh, expires, null, PlayerMapper.ToSummary(player));
    }

    private static AuthResultDto Fail(string err) =>
        new(false, null, null, null, err, null);
}
