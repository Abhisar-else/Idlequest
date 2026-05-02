using IdleQuest.Domain.Enums;
using IdleQuest.Domain.Events;
using IdleQuest.Domain.Supporting;
using IdleQuest.Domain.ValueObjects;

namespace IdleQuest.Domain.Aggregates;

public class Player
{
    private readonly List<DomainEvent> _events = new();

    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string HeroName { get; set; } = "";
    public CharacterClass Class { get; set; }
    public int Level { get; set; } = 1;
    public long Experience { get; set; }
    public long ExperienceToNext { get; set; } = 420;
    public int PrestigeLevel { get; set; }
    public StatBlock BaseStats { get; set; } = StatBlock.Zero;
    public long Gold { get; set; }
    public int CurrentHp { get; set; }
    public int CurrentZoneId { get; set; } = 1;
    public DateTime LastLoginAt { get; set; }
    public int EnemiesSlain { get; set; }
    public int ItemsFound { get; set; }

    public List<InventoryEntry> Inventory { get; set; } = new();
    public EquipmentState Equipment { get; set; } = new();
    public List<PlayerQuestState> ActiveQuests { get; set; } = new();
    public List<SkillEntry> Skills { get; set; } = new();

    public IReadOnlyCollection<DomainEvent> DomainEvents => _events;
    public void ClearEvents() => _events.Clear();
    private void Raise(DomainEvent e) => _events.Add(e);

    public static Player Create(Guid id, string username, string passwordHash, string heroName, CharacterClass cls)
    {
        var stats = cls switch
        {
            CharacterClass.Warrior => new StatBlock(120, 18, 12, 10, 6, 0.05f, 1.5f),
            CharacterClass.Mage => new StatBlock(80, 10, 10, 8, 26, 0.06f, 1.55f),
            CharacterClass.Rogue => new StatBlock(95, 16, 10, 16, 8, 0.10f, 1.65f),
            _ => throw new DomainException("Unknown character class.")
        };

        var p = new Player
        {
            Id = id,
            Username = username,
            PasswordHash = passwordHash,
            HeroName = heroName,
            Class = cls,
            Level = 1,
            Experience = 0,
            ExperienceToNext = 420,
            BaseStats = stats,
            Gold = 120,
            CurrentHp = stats.MaxHp,
            CurrentZoneId = 1,
            LastLoginAt = DateTime.UtcNow,
            Inventory = CreateStarterInventory(),
            Equipment = new EquipmentState(),
            ActiveQuests = new List<PlayerQuestState>(),
            Skills = new List<SkillEntry>()
        };

        var w = p.Inventory.FirstOrDefault(i => i.Slot == ItemSlot.Weapon);
        var a = p.Inventory.FirstOrDefault(i => i.Slot == ItemSlot.Armor);
        if (w is not null) p.Equipment.WeaponId = w.Id;
        if (a is not null) p.Equipment.ArmorId = a.Id;

        p.CurrentHp = p.EffectiveStats().MaxHp;

        return p;
    }

    private static List<InventoryEntry> CreateStarterInventory()
    {
        return new List<InventoryEntry>
        {
            NewInv(Guid.NewGuid(), "Rusty Sword", ItemSlot.Weapon, ItemRarity.Common, 5, "⚔️"),
            NewInv(Guid.NewGuid(), "Cloth Tunic", ItemSlot.Armor, ItemRarity.Common, 4, "🧥")
        };
    }

    private static InventoryEntry NewInv(Guid id, string name, ItemSlot slot, ItemRarity rarity, int bonus, string emoji) =>
        new()
        {
            Id = id,
            Name = name,
            Slot = slot,
            Rarity = rarity,
            Bonus = bonus,
            Emoji = emoji
        };

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public StatBlock EffectiveStats()
    {
        var eq = StatBlock.Zero;
        foreach (var item in Inventory)
        {
            if (item.Id != Equipment.WeaponId && item.Id != Equipment.ArmorId) continue;

            eq = item.Slot switch
            {
                ItemSlot.Weapon => eq.Add(new StatBlock(0, item.Bonus, 0, 0, 0, 0, 1f)),
                ItemSlot.Armor => eq.Add(new StatBlock(0, 0, item.Bonus, 0, 0, 0, 1f)),
                ItemSlot.Accessory => eq.Add(new StatBlock(0, item.Bonus / 2, item.Bonus / 2, 0, 0, 0.01f, 1f)),
                _ => eq
            };
        }

        return BaseStats.Add(eq);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name required.");
        HeroName = name.Trim();
    }

    public void ChangeClass(CharacterClass cls)
    {
        Class = cls;
        var s = cls switch
        {
            CharacterClass.Warrior => new StatBlock(120, 18, 12, 10, 6, 0.05f, 1.5f),
            CharacterClass.Mage => new StatBlock(80, 10, 10, 8, 26, 0.06f, 1.55f),
            CharacterClass.Rogue => new StatBlock(95, 16, 10, 16, 8, 0.10f, 1.65f),
            _ => BaseStats
        };
        BaseStats = s;
        var eff = EffectiveStats();
        CurrentHp = Math.Min(CurrentHp, eff.MaxHp);
        if (CurrentHp <= 0) CurrentHp = eff.MaxHp;
    }

    public void GainExperience(long amount)
    {
        if (amount <= 0) return;
        Experience += amount;
        while (Experience >= ExperienceToNext)
        {
            Experience -= ExperienceToNext;
            LevelUp();
        }
    }

    private void LevelUp()
    {
        Level++;
        ExperienceToNext = Math.Max(100, (long)(ExperienceToNext * 1.45));
        var bonusHp = (int)(BaseStats.MaxHp * 0.15);
        var bonusAtk = 4 + PrestigeLevel;
        var bonusDef = 2 + PrestigeLevel / 2;
        BaseStats = BaseStats.Add(new StatBlock(bonusHp, bonusAtk, bonusDef, 1, 1, 0.002f, 1.01f));
        var eff = EffectiveStats();
        CurrentHp = eff.MaxHp;
        Raise(new PlayerLeveledUp(Id, Level, new StatBlock(bonusHp, bonusAtk, bonusDef, 0, 0, 0, 1f)));
    }

    public void AddGold(long g) => Gold = Math.Max(0, Gold + g);

    public void Prestige()
    {
        if (Level < 50) throw new DomainException("Reach level 50 to prestige.");
        PrestigeLevel++;
        Level = 1;
        Experience = 0;
        ExperienceToNext = 420;
        Gold = Math.Max(Gold, 120);
        BaseStats = Class switch
        {
            CharacterClass.Warrior => new StatBlock(120, 18 + PrestigeLevel * 2, 12, 10, 6, 0.05f, 1.5f),
            CharacterClass.Mage => new StatBlock(80, 10 + PrestigeLevel * 2, 10, 8, 26 + PrestigeLevel, 0.06f, 1.55f),
            CharacterClass.Rogue => new StatBlock(95, 16 + PrestigeLevel * 2, 10, 16, 8, 0.10f, 1.65f),
            _ => BaseStats
        };
        CurrentHp = EffectiveStats().MaxHp;
        Raise(new PrestigeCompleted(Id, PrestigeLevel));
    }

    public InventoryEntry? FindInventory(Guid itemId) =>
        Inventory.FirstOrDefault(i => i.Id == itemId);

    public void Equip(Guid itemId)
    {
        var item = FindInventory(itemId) ?? throw new DomainException("Item not found.");
        if (item.Slot == ItemSlot.Weapon) Equipment.WeaponId = item.Id;
        else if (item.Slot == ItemSlot.Armor) Equipment.ArmorId = item.Id;
        else if (item.Slot == ItemSlot.Accessory) { /* optional third slot — wear as atk/def split */ }
        else throw new DomainException("Cannot equip this item type.");
    }

    public void Unequip(ItemSlot slot)
    {
        switch (slot)
        {
            case ItemSlot.Weapon: Equipment.WeaponId = null; break;
            case ItemSlot.Armor: Equipment.ArmorId = null; break;
            default: throw new DomainException("Invalid equipment slot.");
        }
    }

    public long SellItem(Guid itemId)
    {
        var item = FindInventory(itemId) ?? throw new DomainException("Item not found.");
        if (item.Id == Equipment.WeaponId) Equipment.WeaponId = null;
        if (item.Id == Equipment.ArmorId) Equipment.ArmorId = null;
        Inventory.RemoveAll(i => i.Id == itemId);
        var price = 10 + item.Bonus * (item.Rarity switch
        {
            ItemRarity.Common => 2,
            ItemRarity.Uncommon => 4,
            ItemRarity.Rare => 8,
            ItemRarity.Epic => 14,
            ItemRarity.Legendary => 22,
            _ => 2
        });
        AddGold(price);
        return price;
    }
}
