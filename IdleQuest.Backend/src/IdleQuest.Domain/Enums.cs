namespace IdleQuest.Domain.Enums;

public enum CharacterClass
{
    Warrior = 0,
    Mage = 1,
    Rogue = 2
}

public enum ItemRarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4
}

public enum ItemSlot
{
    Weapon = 0,
    Armor = 1,
    Accessory = 2,
    Consumable = 3
}

public enum CombatResult
{
    Ongoing = 0,
    Victory = 1,
    Defeat = 2,
    Fled = 3
}

public enum SaveSlot
{
    Auto = 0,
    Manual1 = 1,
    Manual2 = 2,
    Manual3 = 3
}
