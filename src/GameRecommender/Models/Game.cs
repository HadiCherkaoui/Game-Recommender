using System.ComponentModel.DataAnnotations;

namespace GameRecommender.Models;

public class Game
{
    public int Id { get; set; }
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Genres (comma-separated)")]
    public string Genre { get; set; } = string.Empty;
    
    public string Platform { get; set; } = string.Empty;
    
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }
    
    [Display(Name = "Image URL")]
    public string ImageUrl { get; set; } = string.Empty;
    
    public string LogoUrl { get; set; } = string.Empty;
} 