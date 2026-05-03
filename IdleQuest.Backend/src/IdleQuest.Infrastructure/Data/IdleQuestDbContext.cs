using IdleQuest.Domain.Aggregates;
using IdleQuest.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace IdleQuest.Infrastructure.Data;

public class IdleQuestDbContext : DbContext
{
    public IdleQuestDbContext(DbContextOptions<IdleQuestDbContext> opts) : base(opts) { }

    public DbSet<Player> Players => Set<Player>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Quest> Quests => Set<Quest>();
    public DbSet<Enemy> Enemies => Set<Enemy>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Npc> Npcs => Set<Npc>();
    public DbSet<CombatSession> CombatSessions => Set<CombatSession>();
    public DbSet<SaveGame> SaveGames => Set<SaveGame>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Player>(e =>
        {
            e.Ignore(p => p.DomainEvents);
            e.HasKey(p => p.Id);
            e.Property(p => p.Username).HasMaxLength(64).IsRequired();
            e.Property(p => p.HeroName).HasMaxLength(64).IsRequired();
            e.Property(p => p.PasswordHash).HasMaxLength(200).IsRequired();
            e.HasIndex(p => p.Username).IsUnique();

            e.OwnsOne(p => p.BaseStats, s =>
            {
                s.Property(x => x.MaxHp).HasColumnName("BaseMaxHp");
                s.Property(x => x.Attack).HasColumnName("BaseAtk");
                s.Property(x => x.Defense).HasColumnName("BaseDef");
                s.Property(x => x.Speed).HasColumnName("BaseSpd");
                s.Property(x => x.MagicPower).HasColumnName("BaseMag");
                s.Property(x => x.CritChance).HasColumnName("BaseCrit");
                s.Property(x => x.CritMultiplier).HasColumnName("BaseCritMult");
            });

            e.OwnsMany(p => p.Inventory);
            e.OwnsOne(p => p.Equipment);
            e.OwnsMany(p => p.ActiveQuests, o => o.ToJson());
            e.OwnsMany(p => p.Skills);
        });

        mb.Entity<Item>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Name).HasMaxLength(120).IsRequired();
            e.OwnsOne(i => i.StatBonus, s =>
            {
                s.Property(x => x.MaxHp).HasColumnName("BonusHp");
                s.Property(x => x.Attack).HasColumnName("BonusAtk");
                s.Property(x => x.Defense).HasColumnName("BonusDef");
                s.Property(x => x.Speed).HasColumnName("BonusSpd");
                s.Property(x => x.MagicPower).HasColumnName("BonusMag");
                s.Property(x => x.CritChance).HasColumnName("BonusCrit");
                s.Property(x => x.CritMultiplier).HasColumnName("BonusCritMult");
            });
        });

        mb.Entity<Quest>(e =>
        {
            e.HasKey(q => q.Id);
            e.Property(q => q.Title).HasMaxLength(200).IsRequired();
            e.OwnsMany(q => q.Objectives);
        });

        mb.Entity<Enemy>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.OwnsOne(x => x.Stats, s =>
            {
                s.Property(z => z.MaxHp).HasColumnName("EnemyMaxHp");
                s.Property(z => z.Attack).HasColumnName("EnemyAtk");
                s.Property(z => z.Defense).HasColumnName("EnemyDef");
                s.Property(z => z.Speed).HasColumnName("EnemySpd");
                s.Property(z => z.MagicPower).HasColumnName("EnemyMag");
                s.Property(z => z.CritChance).HasColumnName("EnemyCrit");
                s.Property(z => z.CritMultiplier).HasColumnName("EnemyCritMult");
            });
            e.OwnsMany(x => x.LootTable, o => o.ToJson());
        });

        mb.Entity<Zone>(e =>
        {
            e.HasKey(z => z.Id);
            e.Property(z => z.Name).HasMaxLength(120).IsRequired();
        });

        mb.Entity<Npc>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Name).HasMaxLength(120).IsRequired();
            e.OwnsMany(n => n.Dialogue, d => d.ToJson());
            e.OwnsMany(n => n.Shop, s => s.ToJson());
        });

        mb.Entity<CombatSession>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.PlayerId);
            e.OwnsMany(c => c.Log, l => l.ToJson());
        });

        mb.Entity<SaveGame>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => new { s.PlayerId, s.Slot }).IsUnique();
            e.Property(s => s.Snapshot).HasColumnType("TEXT");
        });
    }
}
