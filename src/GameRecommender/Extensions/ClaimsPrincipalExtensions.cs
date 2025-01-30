using System.Security.Claims;

namespace GameRecommender.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string? GetSteamId(this ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?.Replace("https://steamcommunity.com/openid/id/", "");
        }
    }
} 