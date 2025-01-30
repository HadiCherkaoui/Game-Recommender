namespace GameRecommender.Models.Recommendations;

public class QuestionAnswer
{
    public string Text { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public Dictionary<string, string> CriteriaUpdates { get; set; } = new();
} 