namespace IdleQuest.Application.DTOs;

public sealed record ZoneDto(int Id, string Name, string Emoji, int RecommendedLevel, bool Unlocked, bool Active);

public sealed record WorldEventDto(string Id, string Title, string Description, DateTime EndsAtUtc);
