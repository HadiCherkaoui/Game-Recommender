namespace GameRecommender.Models.Domain;

public class Game
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public string Developer { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    
    // Steam specific
    public long? SteamAppId { get; set; }
    
    // Navigation properties
    public ICollection<UserRating> UserRatings { get; set; } = new List<UserRating>();
} 