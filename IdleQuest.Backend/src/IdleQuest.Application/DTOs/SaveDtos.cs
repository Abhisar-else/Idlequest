using IdleQuest.Domain.Enums;

namespace IdleQuest.Application.DTOs;

public sealed record SaveSlotDto(SaveSlot Slot, string Label, DateTime? SavedAtUtc, int Version);
