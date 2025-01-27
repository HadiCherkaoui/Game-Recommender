namespace GameRecommender.Models.Domain;

public class UserRating
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int Rating { get; set; }  // 1-5 stars
    public string? Review { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public Game? Game { get; set; }
} 