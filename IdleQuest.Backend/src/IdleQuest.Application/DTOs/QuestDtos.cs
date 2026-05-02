namespace IdleQuest.Application.DTOs;

public sealed record QuestDto(
    Guid Id,
    string Title,
    int RequiredLevel,
    long GoldReward,
    long XpReward,
    IReadOnlyList<string> Objectives);
