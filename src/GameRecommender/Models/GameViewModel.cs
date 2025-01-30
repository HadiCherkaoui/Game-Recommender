using System.Collections.Generic;
using GameRecommender.Models;

namespace GameRecommender.Models
{
    public class GameViewModel
    {
        public List<Game> LocalGames { get; set; } = new();
        public List<SteamGame> SteamGames { get; set; } = new();
        public string DebugInfo { get; set; } = string.Empty;
    }
} 