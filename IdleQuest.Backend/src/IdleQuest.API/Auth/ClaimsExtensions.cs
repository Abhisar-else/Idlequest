using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace IdleQuest.API.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Resolves player id from JWT (sub / NameIdentifier).</summary>
    public static Guid GetPlayerId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                 ?? user.FindFirstValue("playerId");
        if (string.IsNullOrEmpty(id)) throw new UnauthorizedAccessException("Missing player identity.");
        return Guid.Parse(id);
    }
}
