using IdleQuest.Domain.Events;

namespace IdleQuest.Application.Interfaces.Services;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<DomainEvent> events, CancellationToken ct);
}
