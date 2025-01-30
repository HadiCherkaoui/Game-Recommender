namespace GameRecommender.Models;
using System.Text.Json.Serialization;

public class SteamUser
{
    public string SteamId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("avatarfull")]
    public string AvatarUrl { get; set; } = string.Empty;
    [JsonPropertyName("profileurl")]
    public string ProfileUrl { get; set; } = string.Empty;
} 