using IdleQuest.Application.DTOs;
using IdleQuest.Domain.Aggregates;
using IdleQuest.Domain.Enums;

namespace IdleQuest.Application.Mapping;

public static class PlayerMapper
{
    public static string ClassEmoji(CharacterClass c) => c switch
    {
        CharacterClass.Warrior => "🛡️",
        CharacterClass.Mage => "🔥",
        CharacterClass.Rogue => "🗡️",
        _ => "⭐"
    };

    public static PlayerSummaryDto ToSummary(Player p, string? zoneName = null)
    {
        var st = p.EffectiveStats();
        return new PlayerSummaryDto(
            p.Id,
            p.Username,
            p.HeroName,
            p.Class.ToString(),
            ClassEmoji(p.Class),
            p.Level,
            p.Experience,
            p.ExperienceToNext,
            p.PrestigeLevel,
            st.Attack,
            st.Defense,
            st.MaxHp,
            p.CurrentHp,
            p.Gold,
            p.CurrentZoneId,
            zoneName ?? "",
            p.EnemiesSlain,
            p.ItemsFound);
    }
}
