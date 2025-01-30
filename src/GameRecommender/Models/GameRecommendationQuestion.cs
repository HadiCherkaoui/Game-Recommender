namespace GameRecommender.Models;

public class GameRecommendationQuestion
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<QuestionAnswer> Answers { get; set; } = new();
}

public class QuestionAnswer
{
    public string Text { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public Dictionary<string, string> CriteriaUpdates { get; set; } = new();
} 