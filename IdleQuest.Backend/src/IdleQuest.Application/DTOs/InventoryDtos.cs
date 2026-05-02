namespace IdleQuest.Application.DTOs;

public sealed record InventoryItemDto(
    Guid Id,
    string Name,
    string Slot,
    string Rarity,
    int Bonus,
    string Emoji);

public sealed record EquippedDto(InventoryItemDto? Weapon, InventoryItemDto? Armor);
