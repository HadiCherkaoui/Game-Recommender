namespace GameRecommender.Models.Recommendations;

public class GameRecommendationQuestion
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<QuestionAnswer> Answers { get; set; } = new();
} 