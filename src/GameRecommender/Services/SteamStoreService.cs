using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using GameRecommender.Models;
using Microsoft.EntityFrameworkCore;
using GameRecommender.Data;

namespace GameRecommender.Services;

public class SteamStoreService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SteamStoreService> _logger;
    private readonly ApplicationDbContext _context;
    private const int CacheExpirationHours = 24;
    private readonly HashSet<string> _excludedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        // Steam Features
        "Steam Cloud",
        "Steam Achievements",
        "Steam Trading Cards",
        "Steam Workshop",
        "Steam Leaderboards",
        "Includes Source SDK",
        "Steam Cloud",
        "Stats",
        "Valve Anti-Cheat enabled",
        "Steam Turn Notifications",
        "SteamVR Collectibles",

        // Controller Support
        "Full controller support",
        "Partial Controller Support",
        "Controller Friendly",
        
        // Remote Play
        "Remote Play on Tablet",
        "Remote Play on TV",
        "Remote Play on Phone",
        "Remote Play Together",
        "Remote Play",
        
        // Multiplayer Technical
        "Cross-Platform Multiplayer",
        "Online PvP",
        "LAN PvP",
        "Valve Anti-Cheat enabled",
        
        // Accessibility
        "Captions available",
        "Subtitles",
        "In-App Purchases",
        
        // Family Features
        "Family Sharing",
        "Family View",
        
        // Technical Features
        "Includes level editor",
        "Level Editor",
        "Commentary available",
        "Benchmarking",
        "Cloud Gaming",
        "Cloud Saves",
        "HDR",
        "Native Steam Deck support",
        "Steam Deck Playable",
        "Steam Deck Unsupported",
        "Steam Deck Verified",
        "Shared/Split Screen",
        "Shared/Split Screen PvP",
        "Shared/Split Screen Co-Op",
        "Includes Source SDK",
        "Overlay"
    };

    public SteamStoreService(
        IHttpClientFactory httpClientFactory,
        ILogger<SteamStoreService> logger,
        ApplicationDbContext context)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _context = context;
    }

    public async Task<List<string>> GetGameTagsAsync(int appId)
    {
        var cached = await _context.SteamGameDetails
            .FirstOrDefaultAsync(g => g.AppId == appId);

        if (cached != null && cached.LastUpdated > DateTime.UtcNow.AddHours(-CacheExpirationHours))
        {
            return cached.Tags
                .Concat(cached.Genres)
                .Concat(cached.Categories)
                .Distinct()
                .Where(tag => !_excludedTags.Contains(tag))
                .ToList();
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(
                $"https://store.steampowered.com/api/appdetails?appids={appId}&cc=us&l=english");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);
            
            if (json.RootElement.TryGetProperty(appId.ToString(), out var appData) &&
                appData.GetProperty("success").GetBoolean())
            {
                var data = appData.GetProperty("data");
                var details = cached ?? new SteamGameDetails { AppId = appId };
                
                details.Name = data.GetProperty("name").GetString() ?? string.Empty;
                details.Categories = GetCategories(data);
                details.Genres = GetGenres(data);
                details.Tags = await GetStoreTags(appId);  // Get tags from store page
                details.LastUpdated = DateTime.UtcNow;

                if (cached == null)
                    _context.SteamGameDetails.Add(details);
                else
                    _context.SteamGameDetails.Update(details);
                
                await _context.SaveChangesAsync();
                
                return details.Tags
                    .Concat(details.Genres)
                    .Concat(details.Categories)
                    .Distinct()
                    .Where(tag => !_excludedTags.Contains(tag))
                    .ToList();
            }
            
            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching details for app {AppId}", appId);
            return cached?.Tags
                .Concat(cached.Genres)
                .Concat(cached.Categories)
                .Distinct()
                .Where(tag => !_excludedTags.Contains(tag))
                .ToList() 
                   ?? new List<string>();
        }
    }

    private List<string> GetCategories(JsonElement data)
    {
        var categories = new List<string>();
        if (data.TryGetProperty("categories", out var categoriesElement))
        {
            foreach (var category in categoriesElement.EnumerateArray())
            {
                categories.Add(category.GetProperty("description").GetString()?.Trim() ?? string.Empty);
            }
        }
        return categories.Where(c => !string.IsNullOrEmpty(c)).ToList();
    }

    private List<string> GetGenres(JsonElement data)
    {
        var genres = new List<string>();
        if (data.TryGetProperty("genres", out var genresElement))
        {
            foreach (var genre in genresElement.EnumerateArray())
            {
                genres.Add(genre.GetProperty("description").GetString()?.Trim() ?? string.Empty);
            }
        }
        return genres.Where(g => !string.IsNullOrEmpty(g)).ToList();
    }

    private async Task<List<string>> GetStoreTags(int appId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(
                $"https://store.steampowered.com/app/{appId}?cc=us&l=english");
            
            if (!response.IsSuccessStatusCode) return new List<string>();
            
            var html = await response.Content.ReadAsStringAsync();
            var tags = new List<string>();
            
            // Extract tags from the store page HTML
            var tagStart = html.IndexOf("popular_tags");
            if (tagStart != -1)
            {
                var tagSection = html.Substring(tagStart, 2000);
                var tagMatches = System.Text.RegularExpressions.Regex.Matches(
                    tagSection, 
                    @"app_tag[^>]*>([^<]+)</a>");
                
                tags.AddRange(tagMatches
                    .Select(m => m.Groups[1].Value.Trim())
                    .Where(t => !string.IsNullOrEmpty(t) && !_excludedTags.Contains(t)));
            }
            
            return tags;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching store tags for app {AppId}", appId);
            return new List<string>();
        }
    }
} 