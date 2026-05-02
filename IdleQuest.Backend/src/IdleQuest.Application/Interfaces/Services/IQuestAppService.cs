using IdleQuest.Application.DTOs;

namespace IdleQuest.Application.Interfaces.Services;

public interface IQuestAppService
{
    Task<IReadOnlyList<QuestDto>> AvailableAsync(Guid playerId, CancellationToken ct = default);
    Task<QuestDto?> AcceptAsync(Guid playerId, Guid questId, CancellationToken ct = default);
    Task<PlayerSummaryDto?> CompleteAsync(Guid playerId, Guid questId, CancellationToken ct = default);
}
