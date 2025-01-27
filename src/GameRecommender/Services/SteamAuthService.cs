using GameRecommender.Config;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace GameRecommender.Services;

public class SteamAuthService : ISteamAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SteamConfig _steamConfig;
    private readonly ILogger<SteamAuthService> _logger;

    public SteamAuthService(
        IHttpClientFactory httpClientFactory,
        IOptions<SteamConfig> steamConfig,
        ILogger<SteamAuthService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _steamConfig = steamConfig.Value;
        _logger = logger;
    }

    public async Task<string> GetSteamUserIdAsync()
    {
        // The Steam ID will be obtained from the authentication claims
        // This will be implemented when we handle the authentication callback
        return await Task.FromResult(string.Empty);
    }

    public async Task<IEnumerable<string>> GetUserGamesAsync(string steamId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(
                $"{_steamConfig.BaseUrl}/IPlayerService/GetOwnedGames/v1/?key={_steamConfig.ApiKey}&steamid={steamId}&include_appinfo=1");

            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            // TODO: Parse the JSON response and extract game information
            // This is a placeholder until we create the proper model
            return Array.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user games for Steam ID {SteamId}", steamId);
            throw;
        }
    }
} 