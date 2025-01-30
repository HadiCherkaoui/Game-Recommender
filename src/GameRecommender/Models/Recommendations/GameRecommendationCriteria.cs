namespace GameRecommender.Models.Recommendations;

public class GameRecommendationCriteria
{
    public int? PlayerCount { get; set; }
    public bool? WantMultiplayer { get; set; }
    public bool? WantCoop { get; set; }
    public string? PreferredGenre { get; set; }
    public bool? WantStoryRich { get; set; }
    public string? GameMood { get; set; }
} 