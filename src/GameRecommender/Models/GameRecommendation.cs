using System.ComponentModel.DataAnnotations;

namespace GameRecommender.Models;

public class GameRecommendationCriteria
{
    public int? PlayerCount { get; set; }
    public bool? WantMultiplayer { get; set; }
    public bool? WantCoop { get; set; }
    public string? PreferredGenre { get; set; }
    public int? MaxPlayTime { get; set; }
    public bool? WantStoryRich { get; set; }
    public string? GameMood { get; set; } // e.g., Funny, Dark, Casual, Intense
}

public class GameRecommendationQuestion
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<GameRecommendationAnswer> Answers { get; set; } = new();
    public string? NextQuestionId { get; set; }
}

public class GameRecommendationAnswer
{
    public string Text { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? NextQuestionId { get; set; }
    public Dictionary<string, string> CriteriaUpdates { get; set; } = new();
} 