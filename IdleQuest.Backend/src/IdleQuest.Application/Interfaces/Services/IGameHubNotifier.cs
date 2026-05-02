namespace IdleQuest.Application.Interfaces.Services;

public interface IGameHubNotifier
{
    Task NotifyPlayerAsync(Guid playerId, string method, object payload);
    Task NotifyZoneAsync(int zoneId, string method, object payload);
    Task BroadcastAsync(string method, object payload);
}
