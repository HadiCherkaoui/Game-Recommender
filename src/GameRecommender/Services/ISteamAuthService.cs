namespace GameRecommender.Services;

public interface ISteamAuthService
{
    Task<string> GetSteamUserIdAsync();
    Task<IEnumerable<string>> GetUserGamesAsync(string steamId);
} 