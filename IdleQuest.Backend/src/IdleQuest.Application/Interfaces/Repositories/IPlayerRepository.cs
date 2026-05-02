using IdleQuest.Domain.Aggregates;

namespace IdleQuest.Application.Interfaces.Repositories;

public interface IPlayerRepository
{
    Task<Player?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Player?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<List<Player>> GetTopPlayersAsync(int count, CancellationToken ct = default);
    Task AddAsync(Player player, CancellationToken ct = default);
    Task UpdateAsync(Player player, CancellationToken ct = default);
    Task<bool> ExistsAsync(string username, CancellationToken ct = default);
}
