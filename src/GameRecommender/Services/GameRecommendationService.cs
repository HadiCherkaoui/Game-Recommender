using GameRecommender.Models;

namespace GameRecommender.Services;

public interface IGameRecommendationService
{
    GameRecommendationQuestion GetInitialQuestion();
    GameRecommendationQuestion GetQuestion(string questionId);
    GameRecommendationQuestion? GetNextQuestion(string currentQuestionId, string selectedAnswerId);
    List<SteamGame> GetRecommendations(List<SteamGame> games, GameRecommendationCriteria criteria);
}

public class GameRecommendationService : IGameRecommendationService
{
    private readonly List<GameRecommendationQuestion> _questions;

    public GameRecommendationService()
    {
        _questions = new List<GameRecommendationQuestion>
        {
            new GameRecommendationQuestion
            {
                Id = "player_count",
                Text = "How many players are you looking to play with?",
                Answers = new List<QuestionAnswer>
                {
                    new() { Text = "Just me", Value = "1", CriteriaUpdates = new() { { "PlayerCount", "1" } } },
                    new() { Text = "2 players", Value = "2", CriteriaUpdates = new() { { "PlayerCount", "2" } } },
                    new() { Text = "3-4 players", Value = "4", CriteriaUpdates = new() { { "PlayerCount", "4" } } },
                    new() { Text = "5+ players", Value = "5", CriteriaUpdates = new() { { "PlayerCount", "5" } } }
                }
            },
            new GameRecommendationQuestion
            {
                Id = "game_type",
                Text = "What type of game experience are you looking for?",
                Answers = new List<QuestionAnswer>
                {
                    new() { Text = "Single-player story", Value = "single", 
                           CriteriaUpdates = new() { { "WantMultiplayer", "false" }, { "WantStoryRich", "true" } } },
                    new() { Text = "Competitive multiplayer", Value = "competitive", 
                           CriteriaUpdates = new() { { "WantMultiplayer", "true" }, { "WantCoop", "false" } } },
                    new() { Text = "Cooperative play", Value = "coop", 
                           CriteriaUpdates = new() { { "WantMultiplayer", "true" }, { "WantCoop", "true" } } }
                }
            },
            new GameRecommendationQuestion
            {
                Id = "genre",
                Text = "What genre interests you?",
                Answers = new List<QuestionAnswer>
                {
                    new() { Text = "Action/Shooter", Value = "action", 
                           CriteriaUpdates = new() { { "PreferredGenre", "action" } } },
                    new() { Text = "Strategy/Puzzle", Value = "strategy", 
                           CriteriaUpdates = new() { { "PreferredGenre", "strategy" } } },
                    new() { Text = "RPG/Adventure", Value = "rpg", 
                           CriteriaUpdates = new() { { "PreferredGenre", "rpg" } } },
                    new() { Text = "Sports/Racing", Value = "sports", 
                           CriteriaUpdates = new() { { "PreferredGenre", "sports" } } }
                }
            },
            new GameRecommendationQuestion
            {
                Id = "mood",
                Text = "What kind of mood are you in?",
                Answers = new List<QuestionAnswer>
                {
                    new() { Text = "Intense/Competitive", Value = "intense", 
                           CriteriaUpdates = new() { { "GameMood", "intense" } } },
                    new() { Text = "Relaxing/Casual", Value = "relaxing", 
                           CriteriaUpdates = new() { { "GameMood", "relaxing" } } },
                    new() { Text = "Story/Immersive", Value = "immersive", 
                           CriteriaUpdates = new() { { "GameMood", "immersive" } } }
                }
            }
        };
    }

    public GameRecommendationQuestion GetInitialQuestion() => _questions[0];

    public GameRecommendationQuestion GetQuestion(string questionId)
    {
        return _questions.First(q => q.Id == questionId);
    }

    public GameRecommendationQuestion? GetNextQuestion(string currentQuestionId, string selectedAnswerId)
    {
        var currentIndex = _questions.FindIndex(q => q.Id == currentQuestionId);
        if (currentIndex < 0 || currentIndex >= _questions.Count - 1)
            return null;
            
        return _questions[currentIndex + 1];
    }

    public List<SteamGame> GetRecommendations(List<SteamGame> games, GameRecommendationCriteria criteria)
    {
        var recommendations = games.AsEnumerable();

        // Filter by player count
        if (criteria.PlayerCount.HasValue)
        {
            if (criteria.PlayerCount == 1)
            {
                recommendations = recommendations.Where(g => 
                    g.Tags.Any(t => t.Contains("Single", StringComparison.OrdinalIgnoreCase) || 
                                  t.Contains("Singleplayer", StringComparison.OrdinalIgnoreCase)));
            }
            else
            {
                recommendations = recommendations.Where(g => 
                    g.Tags.Any(t => t.Contains("Multi", StringComparison.OrdinalIgnoreCase) || 
                                  t.Contains("Multiplayer", StringComparison.OrdinalIgnoreCase)));
            }
        }

        // Filter by multiplayer preference
        if (criteria.WantMultiplayer.HasValue)
        {
            if (criteria.WantMultiplayer.Value)
            {
                recommendations = recommendations.Where(g => 
                    g.Tags.Any(t => t.Contains("Multi", StringComparison.OrdinalIgnoreCase) || 
                                  t.Contains("Multiplayer", StringComparison.OrdinalIgnoreCase)));

                if (criteria.WantCoop.HasValue)
                {
                    if (criteria.WantCoop.Value)
                    {
                        recommendations = recommendations.Where(g => 
                            g.Tags.Any(t => t.Contains("Co-op", StringComparison.OrdinalIgnoreCase) || 
                                          t.Contains("Coop", StringComparison.OrdinalIgnoreCase)));
                    }
                    else
                    {
                        recommendations = recommendations.Where(g => 
                            g.Tags.Any(t => t.Contains("PvP", StringComparison.OrdinalIgnoreCase) || 
                                          t.Contains("Competitive", StringComparison.OrdinalIgnoreCase)));
                    }
                }
            }
        }

        // Filter by genre
        if (!string.IsNullOrEmpty(criteria.PreferredGenre))
        {
            var genreKeywords = GetGenreKeywords(criteria.PreferredGenre);
            recommendations = recommendations.Where(g => 
                g.Tags.Any(t => genreKeywords.Any(k => 
                    t.Contains(k, StringComparison.OrdinalIgnoreCase))));
        }

        // Filter by story preference
        if (criteria.WantStoryRich.HasValue && criteria.WantStoryRich.Value)
        {
            recommendations = recommendations.Where(g => 
                g.Tags.Any(t => t.Contains("Story", StringComparison.OrdinalIgnoreCase) || 
                              t.Contains("Story Rich", StringComparison.OrdinalIgnoreCase) ||
                              t.Contains("Narrative", StringComparison.OrdinalIgnoreCase)));
        }

        // Filter by mood
        if (!string.IsNullOrEmpty(criteria.GameMood))
        {
            var moodKeywords = GetMoodKeywords(criteria.GameMood);
            recommendations = recommendations.Where(g => 
                g.Tags.Any(t => moodKeywords.Any(k => 
                    t.Contains(k, StringComparison.OrdinalIgnoreCase))));
        }

        return recommendations.ToList();
    }

    private string[] GetGenreKeywords(string genre) => genre.ToLower() switch
    {
        "action" => new[] { "Action", "Shooter", "FPS", "Fighting", "Combat" },
        "strategy" => new[] { "Strategy", "Puzzle", "Tactical", "Turn-Based", "RTS" },
        "rpg" => new[] { "RPG", "Role-Playing", "Adventure", "Open World" },
        "sports" => new[] { "Sports", "Racing", "Football", "Soccer", "Basketball" },
        _ => Array.Empty<string>()
    };

    private string[] GetMoodKeywords(string mood) => mood.ToLower() switch
    {
        "intense" => new[] { "Action", "Competitive", "Fast-Paced", "Difficult", "Challenge" },
        "relaxing" => new[] { "Casual", "Relaxing", "Peaceful", "Chill" },
        "immersive" => new[] { "Story Rich", "Atmospheric", "Immersive", "Adventure" },
        _ => Array.Empty<string>()
    };
} 