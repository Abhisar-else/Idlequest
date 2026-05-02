using IdleQuest.Application.Interfaces.Services;
using IdleQuest.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IdleQuest.Infrastructure.Events;

public interface IDomainEventHandler<in TEvent> where TEvent : DomainEvent
{
    Task HandleAsync(TEvent e, CancellationToken ct = default);
}

public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<DomainEventDispatcher> _log;

    public DomainEventDispatcher(IServiceProvider sp, ILogger<DomainEventDispatcher> log)
    {
        _sp = sp;
        _log = log;
    }

    public async Task DispatchAsync(IEnumerable<DomainEvent> events, CancellationToken ct)
    {
        foreach (var e in events)
        {
            try
            {
                var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(e.GetType());
                var enumerableType = typeof(IEnumerable<>).MakeGenericType(handlerType);
                var handlers = _sp.GetService(enumerableType) as IEnumerable<object>;
                if (handlers is null) continue;

                foreach (var handler in handlers)
                {
                    var method = handlerType.GetMethod("HandleAsync")!;
                    await ((Task)method.Invoke(handler, new object[] { e, ct })!)!;
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Dispatch failed for {Event}", e.GetType().Name);
            }
        }
    }
}

public sealed class PlayerLeveledUpHandler : IDomainEventHandler<PlayerLeveledUp>
{
    private readonly IGameHubNotifier _hub;

    public PlayerLeveledUpHandler(IGameHubNotifier hub) => _hub = hub;

    public Task HandleAsync(PlayerLeveledUp e, CancellationToken ct = default) =>
        _hub.NotifyPlayerAsync(e.PlayerId, "LevelUp", new
        {
            e.NewLevel,
            BonusHp = e.BonusStats.MaxHp,
            BonusAtk = e.BonusStats.Attack,
            BonusDef = e.BonusStats.Defense
        });
}

public sealed class PrestigeCompletedHandler : IDomainEventHandler<PrestigeCompleted>
{
    private readonly IGameHubNotifier _hub;

    public PrestigeCompletedHandler(IGameHubNotifier hub) => _hub = hub;

    public Task HandleAsync(PrestigeCompleted e, CancellationToken ct = default) =>
        _hub.NotifyPlayerAsync(e.PlayerId, "Prestige", new { e.NewPrestigeLevel, Message = "Prestige complete!" });
}

public static class DomainEventServiceCollectionExtensions
{
    public static IServiceCollection AddIdleQuestDomainEvents(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IDomainEventHandler<PlayerLeveledUp>, PlayerLeveledUpHandler>();
        services.AddScoped<IDomainEventHandler<PrestigeCompleted>, PrestigeCompletedHandler>();
        return services;
    }
}
