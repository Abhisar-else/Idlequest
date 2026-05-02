namespace IdleQuest.Domain.ValueObjects;

public sealed record StatBlock(
    int MaxHp,
    int Attack,
    int Defense,
    int Speed,
    int MagicPower,
    float CritChance,
    float CritMultiplier)
{
    public static StatBlock Zero => new(0, 0, 0, 0, 0, 0, 1f);

    public StatBlock Add(StatBlock o) => new(
        MaxHp + o.MaxHp,
        Attack + o.Attack,
        Defense + o.Defense,
        Speed + o.Speed,
        MagicPower + o.MagicPower,
        CritChance + o.CritChance,
        CritMultiplier * o.CritMultiplier);
}
