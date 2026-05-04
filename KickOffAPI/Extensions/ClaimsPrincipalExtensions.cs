using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace KickOffAPI.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string? GetApplicationUserId(this ClaimsPrincipal principal)
        {
            return principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal.FindFirstValue("sub");
        }
    }
}
