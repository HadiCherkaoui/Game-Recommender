using GameRecommender.Models;

namespace GameRecommender.Services;

public interface IGameRecommendationService
{
    GameRecommendationQuestion GetInitialQuestion();
    GameRecommendationQuestion? GetNextQuestion(string currentQuestionId, string answerId);
    GameRecommendationQuestion GetQuestion(string questionId);
    List<SteamGame> GetRecommendations(List<SteamGame> userGames, GameRecommendationCriteria criteria);
}

public class GameRecommendationService : IGameRecommendationService
{
    private readonly Dictionary<string, GameRecommendationQuestion> _questions;

    public GameRecommendationService()
    {
        _questions = InitializeQuestions();
    }

    public GameRecommendationQuestion GetInitialQuestion()
    {
        return _questions["players"];
    }

    public GameRecommendationQuestion GetQuestion(string questionId)
    {
        return _questions[questionId];
    }

    public GameRecommendationQuestion? GetNextQuestion(string currentQuestionId, string answerId)
    {
        var currentQuestion = _questions[currentQuestionId];
        var selectedAnswer = currentQuestion.Answers.First(a => a.Value == answerId);
        
        return selectedAnswer.NextQuestionId != null ? _questions[selectedAnswer.NextQuestionId] : null;
    }

    public List<SteamGame> GetRecommendations(List<SteamGame> userGames, GameRecommendationCriteria criteria)
    {
        var recommendations = userGames.AsEnumerable();

        // Filter by player count and multiplayer preferences
        if (criteria.PlayerCount.HasValue)
        {
            if (criteria.PlayerCount == 1)
            {
                recommendations = recommendations.Where(g => 
                    g.Tags.Any(t => t.Contains("Single", StringComparison.OrdinalIgnoreCase) || 
                                  t.Contains("Singleplayer", StringComparison.OrdinalIgnoreCase)));
            }
            else if (criteria.WantCoop == true)
            {
                recommendations = recommendations.Where(g => 
                    g.Tags.Any(t => t.Contains("Co-op", StringComparison.OrdinalIgnoreCase) || 
                                  t.Contains("Coop", StringComparison.OrdinalIgnoreCase) ||
                                  t.Contains("Cooperative", StringComparison.OrdinalIgnoreCase)));
            }
            else if (criteria.WantMultiplayer == true)
            {
                recommendations = recommendations.Where(g => 
                    g.Tags.Any(t => t.Contains("Multi", StringComparison.OrdinalIgnoreCase) ||
                                  t.Contains("Multiplayer", StringComparison.OrdinalIgnoreCase) ||
                                  t.Contains("PvP", StringComparison.OrdinalIgnoreCase) ||
                                  t.Contains("Competitive", StringComparison.OrdinalIgnoreCase)));
            }
        }

        // Filter by genre with broader matching
        if (!string.IsNullOrEmpty(criteria.PreferredGenre))
        {
            var genreKeywords = GetGenreKeywords(criteria.PreferredGenre);
            recommendations = recommendations.Where(g => 
                g.Tags.Any(t => genreKeywords.Any(k => 
                    t.Contains(k, StringComparison.OrdinalIgnoreCase))));
        }

        // Filter by story preference
        if (criteria.WantStoryRich == true)
        {
            recommendations = recommendations.Where(g => 
                g.Tags.Any(t => 
                    t.Contains("Story", StringComparison.OrdinalIgnoreCase) ||
                    t.Contains("Atmospheric", StringComparison.OrdinalIgnoreCase) ||
                    t.Contains("Narrative", StringComparison.OrdinalIgnoreCase) ||
                    t.Contains("Soundtrack", StringComparison.OrdinalIgnoreCase)));
        }

        // Filter by game mood with broader matching
        if (!string.IsNullOrEmpty(criteria.GameMood))
        {
            var moodKeywords = GetMoodKeywords(criteria.GameMood);
            recommendations = recommendations.Where(g => 
                g.Tags.Any(t => moodKeywords.Any(k => 
                    t.Contains(k, StringComparison.OrdinalIgnoreCase))));
        }

        return recommendations.ToList();
    }

    private Dictionary<string, GameRecommendationQuestion> InitializeQuestions()
    {
        return new Dictionary<string, GameRecommendationQuestion>
        {
            ["players"] = new GameRecommendationQuestion
            {
                Id = "players",
                Text = "How many players do you want to play with?",
                Answers = new List<GameRecommendationAnswer>
                {
                    new() {
                        Text = "Just me (Single Player)",
                        Value = "1",
                        NextQuestionId = "genre",
                        CriteriaUpdates = new() { ["PlayerCount"] = "1" }
                    },
                    new() {
                        Text = "With friends",
                        Value = "multi",
                        NextQuestionId = "playStyle",
                        CriteriaUpdates = new() { ["PlayerCount"] = "2" }
                    }
                }
            },
            ["playStyle"] = new GameRecommendationQuestion
            {
                Id = "playStyle",
                Text = "How do you want to play with others?",
                Answers = new List<GameRecommendationAnswer>
                {
                    new() {
                        Text = "Cooperatively (Co-op)",
                        Value = "coop",
                        NextQuestionId = "genre",
                        CriteriaUpdates = new() { ["WantCoop"] = "true" }
                    },
                    new() {
                        Text = "Competitively (PvP)",
                        Value = "pvp",
                        NextQuestionId = "genre",
                        CriteriaUpdates = new() { ["WantMultiplayer"] = "true" }
                    }
                }
            },
            ["genre"] = new GameRecommendationQuestion
            {
                Id = "genre",
                Text = "What type of game are you in the mood for?",
                Answers = new List<GameRecommendationAnswer>
                {
                    new() {
                        Text = "Action & Adventure",
                        Value = "action",
                        NextQuestionId = "mood",
                        CriteriaUpdates = new() { ["PreferredGenre"] = "Action" }
                    },
                    new() {
                        Text = "Strategy & Puzzle",
                        Value = "strategy",
                        NextQuestionId = "mood",
                        CriteriaUpdates = new() { ["PreferredGenre"] = "Strategy" }
                    },
                    new() {
                        Text = "RPG & Story",
                        Value = "rpg",
                        NextQuestionId = "mood",
                        CriteriaUpdates = new() { 
                            ["PreferredGenre"] = "RPG",
                            ["WantStoryRich"] = "true"
                        }
                    }
                }
            },
            ["mood"] = new GameRecommendationQuestion
            {
                Id = "mood",
                Text = "What kind of mood are you in?",
                Answers = new List<GameRecommendationAnswer>
                {
                    new() {
                        Text = "Relaxed & Casual",
                        Value = "casual",
                        CriteriaUpdates = new() { ["GameMood"] = "Casual" }
                    },
                    new() {
                        Text = "Fun & Humorous",
                        Value = "funny",
                        CriteriaUpdates = new() { ["GameMood"] = "Funny" }
                    },
                    new() {
                        Text = "Intense & Challenging",
                        Value = "intense",
                        CriteriaUpdates = new() { ["GameMood"] = "Difficult" }
                    }
                }
            }
        };
    }

    private string[] GetGenreKeywords(string genre)
    {
        return genre.ToLower() switch
        {
            "action" => new[] { 
                "action", "shooter", "fps", "fighting", "combat", "war", 
                "tactical", "battle", "military" 
            },
            "strategy" => new[] { 
                "strategy", "puzzle", "tactical", "tower defense", "management",
                "building", "simulation", "rts", "turn-based"
            },
            "rpg" => new[] { 
                "rpg", "role-playing", "role playing", "jrpg", "action rpg",
                "fantasy", "open world", "adventure"
            },
            _ => new[] { genre }
        };
    }

    private string[] GetMoodKeywords(string mood)
    {
        return mood.ToLower() switch
        {
            "casual" => new[] { 
                "casual", "relaxing", "peaceful", "family friendly", 
                "easy", "simple", "laid-back", "chill"
            },
            "funny" => new[] { 
                "funny", "humor", "comedy", "silly", "cute", 
                "party", "fun", "colorful"
            },
            "difficult" => new[] { 
                "difficult", "hard", "challenging", "intense", "hardcore",
                "competitive", "fast-paced", "action", "skill-based"
            },
            _ => new[] { mood }
        };
    }
} 