using IdleQuest.Application.DTOs;

namespace IdleQuest.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResultDto> RegisterAsync(RegisterRequest req, CancellationToken ct = default);
    Task<AuthResultDto> LoginAsync(LoginRequest req, CancellationToken ct = default);
    Task<AuthResultDto> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
}
