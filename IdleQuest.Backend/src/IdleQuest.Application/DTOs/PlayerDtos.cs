using System.Text.Json.Serialization;

namespace IdleQuest.Application.DTOs;

public sealed record PlayerSummaryDto(
    Guid Id,
    string Username,
    string HeroName,
    string Class,
    string ClassEmoji,
    int Level,
    long Experience,
    long ExperienceToNext,
    int PrestigeLevel,
    int Attack,
    int Defense,
    int MaxHp,
    int CurrentHp,
    long Gold,
    int CurrentZoneId,
    string CurrentZoneName,
    int EnemiesSlain,
    int ItemsFound);

public sealed record RenameRequest(string Name);

public sealed record ChangeClassRequest([property: JsonPropertyName("class")] string Class);

public sealed record LeaderboardEntryDto(string HeroName, int Level, int PrestigeLevel, long Gold);
