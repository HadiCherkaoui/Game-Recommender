using GameRecommender.Models;
using Microsoft.Extensions.Caching.Memory;

namespace GameRecommender.Services;

public interface IVotingSessionService
{
    Task<VotingSession> CreateSessionAsync(string creatorId, List<SteamGame> games);
    Task<VotingSession?> GetSessionAsync(string sessionId);
    Task<bool> AddVoteAsync(string sessionId, GameVote vote);
    Task<VotingSessionResult> GetResultsAsync(string sessionId);
}

public class VotingSessionService : IVotingSessionService
{
    private readonly IMemoryCache _cache;
    private const int SessionDurationHours = 24;

    public VotingSessionService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<VotingSession> CreateSessionAsync(string creatorId, List<SteamGame> games)
    {
        var session = new VotingSession
        {
            CreatorId = creatorId,
            Games = games,
            ExpiresAt = DateTime.UtcNow.AddHours(SessionDurationHours)
        };

        _cache.Set(GetCacheKey(session.Id), session, session.ExpiresAt);
        
        return Task.FromResult(session);
    }

    public Task<VotingSession?> GetSessionAsync(string sessionId)
    {
        _cache.TryGetValue(GetCacheKey(sessionId), out VotingSession? session);
        return Task.FromResult(session);
    }

    public async Task<bool> AddVoteAsync(string sessionId, GameVote vote)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null || !session.IsActive) return false;

        // Remove any existing vote by this user for this game
        session.Votes.RemoveAll(v => v.VoterId == vote.VoterId && v.GameAppId == vote.GameAppId);
        
        // Add the new vote
        session.Votes.Add(vote);
        
        // Update the cache
        _cache.Set(GetCacheKey(sessionId), session, session.ExpiresAt);
        
        return true;
    }

    public async Task<VotingSessionResult> GetResultsAsync(string sessionId)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null) 
            return new VotingSessionResult { SessionId = sessionId };

        var results = new VotingSessionResult
        {
            SessionId = sessionId,
            Results = session.Games.Select(game =>
            {
                var gameVotes = session.Votes.Where(v => v.GameAppId == game.AppId).ToList();
                return new GameVoteResult
                {
                    Game = game,
                    AverageRating = gameVotes.Any() ? gameVotes.Average(v => v.Rating) : 0,
                    TotalVotes = gameVotes.Count,
                    VoterRatings = gameVotes.Select(v => new VoterRating
                    {
                        VoterName = v.VoterName,
                        Rating = v.Rating
                    }).ToList()
                };
            })
            .OrderByDescending(r => r.AverageRating)
            .ToList()
        };

        return results;
    }

    private string GetCacheKey(string sessionId) => $"voting_session_{sessionId}";
} 