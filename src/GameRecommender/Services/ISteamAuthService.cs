using System.Collections.Generic;
using System.Threading.Tasks;
using GameRecommender.Models;

namespace GameRecommender.Services;

public interface ISteamAuthService
{
    Task<string> GetSteamUserIdAsync();
    Task<List<SteamGame>> GetUserGamesAsync(string steamId);
    Task<SteamUser> GetUserProfileAsync(string steamId);
} 