using GameRecommender.Config;
using Microsoft.Extensions.Options;
using System.Text.Json;
using GameRecommender.Models;

namespace GameRecommender.Services;

public class SteamAuthService : ISteamAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SteamConfig _steamConfig;
    private readonly ILogger<SteamAuthService> _logger;
    private readonly SteamStoreService _steamStoreService;

    public SteamAuthService(
        IHttpClientFactory httpClientFactory,
        IOptions<SteamConfig> steamConfig,
        ILogger<SteamAuthService> logger,
        SteamStoreService steamStoreService)
    {
        _httpClientFactory = httpClientFactory;
        _steamConfig = steamConfig.Value;
        _logger = logger;
        _steamStoreService = steamStoreService;
    }

    public async Task<string> GetSteamUserIdAsync()
    {
        // The Steam ID will be obtained from the authentication claims
        // This will be implemented when we handle the authentication callback
        return await Task.FromResult(string.Empty);
    }

    public async Task<List<SteamGame>> GetUserGamesAsync(string steamId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(
                $"{_steamConfig.BaseUrl}/IPlayerService/GetOwnedGames/v1/?key={_steamConfig.ApiKey}&steamid={steamId}&include_appinfo=1&include_played_free_games=1");

            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Steam API Response: {Content}", content);
            
            var json = JsonDocument.Parse(content);
            var gamesArray = json.RootElement
                .GetProperty("response")
                .GetProperty("games")
                .EnumerateArray();

            var result = new List<SteamGame>();
            
            // Add delay between requests to avoid rate limiting
            var delayBetweenRequests = TimeSpan.FromMilliseconds(500);

            foreach (var game in gamesArray)
            {
                // Skip games with no playtime (likely hidden/uninstalled)
                if (game.TryGetProperty("playtime_forever", out var playtime) && playtime.GetInt32() == 0)
                {
                    continue;
                }

                var steamGame = new SteamGame
                {
                    AppId = (int)game.GetProperty("appid").GetUInt32(),
                    Name = game.GetProperty("name").GetString() ?? "Unknown Game",
                    PlaytimeMinutes = (int)game.GetProperty("playtime_forever").GetInt32(),
                    LogoUrl = $"https://cdn.akamai.steamstatic.com/steam/apps/{game.GetProperty("appid")}/header.jpg",
                    IconUrl = game.TryGetProperty("img_icon_url", out var icon) ? 
                        $"http://media.steampowered.com/steamcommunity/public/images/apps/{game.GetProperty("appid")}/{icon.GetString()}.jpg" : ""
                };
                
                steamGame.Tags = await _steamStoreService.GetGameTagsAsync(steamGame.AppId);
                result.Add(steamGame);
                
                // Add delay after every 10 requests
                if (result.Count % 10 == 0)
                {
                    await Task.Delay(delayBetweenRequests);
                }
            }

            _logger.LogInformation("Retrieved {Count} games after filtering", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user games for Steam ID {SteamId}", steamId);
            throw;
        }
    }

    public async Task<SteamUser> GetUserProfileAsync(string steamId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(
                $"{_steamConfig.BaseUrl}/ISteamUser/GetPlayerSummaries/v2/?key={_steamConfig.ApiKey}&steamids={steamId}");

            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);
            var player = json.RootElement
                .GetProperty("response")
                .GetProperty("players")
                .EnumerateArray()
                .FirstOrDefault();

            return new SteamUser
            {
                SteamId = steamId,
                Name = player.GetProperty("personaname").GetString() ?? string.Empty,
                AvatarUrl = player.GetProperty("avatarfull").GetString() ?? string.Empty,
                ProfileUrl = player.GetProperty("profileurl").GetString() ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user profile for Steam ID {SteamId}", steamId);
            throw;
        }
    }

    public async Task ImportSteamGamesAsync(string steamId)
    {
        try 
        {
            _logger.LogInformation($"Starting Steam game import for {steamId}");
            
            var response = await _httpClientFactory.CreateClient().GetAsync($"{_steamConfig.BaseUrl}/IPlayerService/GetOwnedGames/v1/?key={_steamConfig.ApiKey}&steamid={steamId}&include_appinfo=1&include_played_free_games=1");
            
            _logger.LogDebug($"Steam API response status: {response.StatusCode}");
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogTrace($"Raw Steam API response: {content}");

            var result = JsonSerializer.Deserialize<SteamGamesResponse>(content);
            
            _logger.LogInformation($"Found {result?.Response.GameCount} games from Steam");
            
            var steamGames = await GetUserGamesAsync(steamId);
            
            // Add your database import logic here
            foreach (var game in steamGames)
            {
                // Create/update local game records
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Steam game import failed");
            throw;
        }
    }
} 