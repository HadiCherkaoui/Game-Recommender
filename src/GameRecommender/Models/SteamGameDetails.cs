using System.Text.Json;

namespace GameRecommender.Models;

public class SteamGameDetails
{
    public int Id { get; set; }
    public int AppId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> Categories { get; set; } = new();
    public List<string> Genres { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public DateTime LastUpdated { get; set; }
} 