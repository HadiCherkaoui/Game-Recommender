namespace GameRecommender.Models;

public class SteamGame
{
    public int AppId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
    public int PlaytimeMinutes { get; set; }
    public string Platform { get; set; } = "PC";
    public List<string> Tags { get; set; } = new();
} 