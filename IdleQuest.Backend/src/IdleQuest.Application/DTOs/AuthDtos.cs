using System.Text.Json.Serialization;

namespace IdleQuest.Application.DTOs;

public sealed record RegisterRequest(
    string Username,
    string Password,
    string HeroName,
    [property: JsonPropertyName("class")] string Class);

public sealed record LoginRequest(string Username, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record AuthResultDto(
    bool Success,
    string? Token,
    string? RefreshToken,
    DateTime? ExpiresAtUtc,
    string? Error,
    PlayerSummaryDto? Player);
