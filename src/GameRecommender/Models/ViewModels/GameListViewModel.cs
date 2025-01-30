namespace GameRecommender.Models.ViewModels;

public class GameListViewModel
{
    public List<SteamGame> SteamGames { get; set; } = new();
    public string DebugInfo { get; set; } = string.Empty;
} 