using IdleQuest.Application.DTOs;

namespace IdleQuest.Application.Interfaces.Services;

public interface IPlayerAppService
{
    Task<PlayerSummaryDto?> GetMeAsync(Guid playerId, CancellationToken ct = default);
    Task<PlayerSummaryDto?> RenameAsync(Guid playerId, RenameRequest req, CancellationToken ct = default);
    Task<PlayerSummaryDto?> ChangeClassAsync(Guid playerId, ChangeClassRequest req, CancellationToken ct = default);
    Task<PlayerSummaryDto?> PrestigeAsync(Guid playerId, CancellationToken ct = default);
    Task<IReadOnlyList<LeaderboardEntryDto>> LeaderboardAsync(CancellationToken ct = default);
}
