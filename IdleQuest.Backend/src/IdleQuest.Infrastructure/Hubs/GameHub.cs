using System.Collections.Concurrent;
using IdleQuest.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace IdleQuest.Infrastructure.Hubs;

[Authorize]
public sealed class GameHub : Hub
{
    private readonly ICombatService _combat;
    private readonly IWorldService _world;

    public static readonly ConcurrentDictionary<Guid, string> PlayerConnections = new();

    public GameHub(ICombatService combat, IWorldService world)
    {
        _combat = combat;
        _world = world;
    }

    public override async Task OnConnectedAsync()
    {
        var id = GetPlayerId();
        PlayerConnections[id] = Context.ConnectionId!;
        await Groups.AddToGroupAsync(Context.ConnectionId!, $"player:{id}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        try
        {
            var id = GetPlayerId();
            PlayerConnections.TryRemove(id, out _);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId!, $"player:{id}");
        }
        catch
        {
            // Connection may drop before auth context is established.
        }

        await base.OnDisconnectedAsync(ex);
    }

    public async Task JoinZone(int zoneId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId!, $"zone:{zoneId}");
        await Clients.Group($"zone:{zoneId}").SendAsync("PlayerEnteredZone", new { PlayerId = GetPlayerId(), zoneId });
    }

    public async Task LeaveZone(int zoneId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId!, $"zone:{zoneId}");
    }

    public async Task SendCombatAction(string action, string sessionId)
    {
        var playerId = GetPlayerId();
        var sid = Guid.Parse(sessionId);
        var act = action.ToLowerInvariant();
        if (act == "attack")
        {
            var state = await _combat.AttackAsync(sid, playerId);
            await Clients.Caller.SendAsync("CombatUpdate", state);
        }
        else if (act == "flee")
        {
            var state = await _combat.FleeAsync(sid, playerId);
            await Clients.Caller.SendAsync("CombatUpdate", state);
        }
    }

    public async Task RequestWorldState()
    {
        var world = await _world.GetWorldStateAsync();
        await Clients.Caller.SendAsync("WorldState", world);
    }

    private Guid GetPlayerId()
    {
        var claim = Context.User?.FindFirst("sub")?.Value
                    ?? Context.User?.FindFirst("playerId")?.Value;
        return Guid.Parse(claim ?? throw new HubException("Unauthorized."));
    }
}

public sealed class GameHubNotifier : IGameHubNotifier
{
    private readonly IHubContext<GameHub> _hub;

    public GameHubNotifier(IHubContext<GameHub> hub) => _hub = hub;

    public Task NotifyPlayerAsync(Guid playerId, string method, object payload) =>
        _hub.Clients.Group($"player:{playerId}").SendAsync(method, payload);

    public Task NotifyZoneAsync(int zoneId, string method, object payload) =>
        _hub.Clients.Group($"zone:{zoneId}").SendAsync(method, payload);

    public Task BroadcastAsync(string method, object payload) =>
        _hub.Clients.All.SendAsync(method, payload);
}
