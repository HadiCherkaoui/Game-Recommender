using System.Collections.Generic;

namespace GameRecommender.Models
{
    public class SteamGamesResponse
    {
        public ResponseData Response { get; set; } = new();
    }

    public class ResponseData
    {
        public int GameCount { get; set; }
        public List<SteamGame> Games { get; set; } = new();
    }
} 