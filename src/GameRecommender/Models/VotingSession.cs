using System.ComponentModel.DataAnnotations;

namespace GameRecommender.Models;

public class VotingSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public string CreatorId { get; set; } = string.Empty;
    public List<SteamGame> Games { get; set; } = new();
    public List<GameVote> Votes { get; set; } = new();
    public bool IsActive => DateTime.UtcNow < ExpiresAt;
}

public class GameVote
{
    public string SessionId { get; set; } = string.Empty;
    public string VoterId { get; set; } = string.Empty;
    public string VoterName { get; set; } = string.Empty;
    public int GameAppId { get; set; }
    public int Rating { get; set; } // 1-10
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;
}

public class VotingSessionResult
{
    public string SessionId { get; set; } = string.Empty;
    public List<GameVoteResult> Results { get; set; } = new();
}

public class GameVoteResult
{
    public SteamGame Game { get; set; } = null!;
    public double AverageRating { get; set; }
    public int TotalVotes { get; set; }
    public List<VoterRating> VoterRatings { get; set; } = new();
}

public class VoterRating
{
    public string VoterName { get; set; } = string.Empty;
    public int Rating { get; set; }
} 