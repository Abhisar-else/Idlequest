namespace IdleQuest.Domain.Events;

public sealed record PrestigeCompleted(Guid PlayerId, int NewPrestigeLevel) : DomainEvent;
