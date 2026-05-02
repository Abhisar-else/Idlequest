using IdleQuest.Domain.Aggregates;
using IdleQuest.Domain.Enums;
using IdleQuest.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IdleQuest.Infrastructure.Data;

public sealed class SeedDataHostedService : IHostedService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<SeedDataHostedService> _log;

    public SeedDataHostedService(IServiceProvider sp, ILogger<SeedDataHostedService> log)
    {
        _sp = sp;
        _log = log;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdleQuestDbContext>();
        await db.Database.EnsureCreatedAsync(ct);

        if (!await db.Zones.AnyAsync(ct))
        {
            db.Zones.AddRange(
                new Zone { Id = 1, Name = "Whispering Woods", Emoji = "🌲", RecommendedLevel = 1, SortOrder = 1 },
                new Zone { Id = 2, Name = "Shadow Forest", Emoji = "🌑", RecommendedLevel = 8, SortOrder = 2 },
                new Zone { Id = 3, Name = "Cursed Ruins", Emoji = "🏛️", RecommendedLevel = 15, SortOrder = 3 },
                new Zone { Id = 4, Name = "Dragon Peaks", Emoji = "🏔️", RecommendedLevel = 27, SortOrder = 4 });
            await db.SaveChangesAsync(ct);
            _log.LogInformation("Seeded zones.");
        }

        if (!await db.Enemies.AnyAsync(ct))
        {
            db.Enemies.AddRange(
                MakeEnemy("Goblin Scout", "👹", 1, 3, new StatBlock(45, 10, 6, 8, 4, 0.05f, 1.4f)),
                MakeEnemy("Forest Troll", "🧌", 1, 8, new StatBlock(90, 18, 14, 6, 5, 0.04f, 1.45f)),
                MakeEnemy("Shadow Wraith", "👻", 2, 12, new StatBlock(110, 22, 12, 14, 16, 0.06f, 1.5f)),
                MakeEnemy("Ruin Guardian", "🗿", 3, 18, new StatBlock(200, 28, 22, 8, 10, 0.05f, 1.55f)),
                MakeEnemy("Young Drake", "🐉", 4, 28, new StatBlock(320, 40, 28, 18, 24, 0.07f, 1.7f)));
            await db.SaveChangesAsync(ct);
            _log.LogInformation("Seeded enemies.");
        }

        if (!await db.Items.AnyAsync(ct))
        {
            var potion = Guid.NewGuid();
            var helm = Guid.NewGuid();
            db.Items.AddRange(
                new Item
                {
                    Id = potion,
                    Name = "Health Potion",
                    Slot = ItemSlot.Consumable,
                    Rarity = ItemRarity.Common,
                    Emoji = "🧪",
                    BuyPrice = 25,
                    StatBonus = new StatBlock(30, 0, 0, 0, 0, 0, 1f)
                },
                new Item
                {
                    Id = helm,
                    Name = "Traveler's Helm",
                    Slot = ItemSlot.Armor,
                    Rarity = ItemRarity.Uncommon,
                    Emoji = "⛑️",
                    BuyPrice = 120,
                    StatBonus = new StatBlock(0, 0, 12, 0, 0, 0, 1f)
                });
            await db.SaveChangesAsync(ct);

            var merchant = Guid.NewGuid();
            db.Npcs.Add(new Npc
            {
                Id = merchant,
                Name = "Merchant Finn",
                ZoneId = 1,
                IsMerchant = true,
                Dialogue = new List<DialogueLine>(),
                Shop = new List<ShopListing>
                {
                    new() { ItemId = potion, Price = 25, Stock = 99 },
                    new() { ItemId = helm, Price = 120, Stock = 5 }
                }
            });

            await db.SaveChangesAsync(ct);
            _log.LogInformation("Seeded items and merchant.");
        }

        if (!await db.Quests.AnyAsync(ct))
        {
            db.Quests.Add(new Quest
            {
                Id = Guid.NewGuid(),
                Title = "First Blood",
                RequiredLevel = 1,
                GoldReward = 40,
                XpReward = 80,
                Objectives = new List<QuestObjective>
                {
                    new() { Id = "kill", Description = "Win one combat encounter", TargetAmount = 1 }
                }
            });
            db.Quests.Add(new Quest
            {
                Id = Guid.NewGuid(),
                Title = "Deep Woods",
                RequiredLevel = 8,
                GoldReward = 200,
                XpReward = 400,
                Objectives = new List<QuestObjective>
                {
                    new() { Id = "visit", Description = "Travel to Shadow Forest", TargetAmount = 1 }
                }
            });
            await db.SaveChangesAsync(ct);
            _log.LogInformation("Seeded quests.");
        }
    }

    private static Enemy MakeEnemy(string name, string emoji, int zoneId, int level, StatBlock stats) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Emoji = emoji,
            ZoneId = zoneId,
            Level = level,
            Stats = stats,
            LootTable = new List<LootRow>()
        };

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
