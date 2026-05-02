using IdleQuest.Domain.ValueObjects;

namespace IdleQuest.Domain.Events;

public sealed record PlayerLeveledUp(Guid PlayerId, int NewLevel, StatBlock BonusStats) : DomainEvent;
