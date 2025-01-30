using GameRecommender.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.SignalR;
using GameRecommender.Hubs;
using System.Threading.Tasks;

namespace GameRecommender.Services;

public interface IVotingSessionService
{
    VotingSession CreateSessionAsync(string creatorId, List<SteamGame> games);
    VotingSession? GetSessionAsync(string sessionId);
    Task<bool> AddVoteAsync(string sessionId, GameVote vote);
    Task<VotingSessionResult> GetResultsAsync(string sessionId);
    Task CleanupExpiredSessionsAsync();
}

public class VotingSessionService : IVotingSessionService
{
    private readonly IMemoryCache _cache;
    private readonly IHubContext<VotingHub> _hubContext;
    private const int SessionDurationMinutes = 5; // Changed from 24 hours to 5 minutes
    private readonly MemoryCacheEntryOptions _cacheOptions;

    public VotingSessionService(IMemoryCache cache, IHubContext<VotingHub> hubContext)
    {
        _cache = cache;
        _hubContext = hubContext;
        _cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(SessionDurationMinutes))
            .RegisterPostEvictionCallback(OnSessionExpired);
    }

    private void OnSessionExpired(object key, object? value, EvictionReason reason, object? state)
    {
        if (value is VotingSession session)
        {
            // Notify clients that the session has expired
            _hubContext.Clients.Group(session.Id)
                .SendAsync("SessionExpired", session.Id).Wait();
        }
    }

    public VotingSession CreateSessionAsync(string creatorId, List<SteamGame> games)
    {
        var session = new VotingSession
        {
            CreatorId = creatorId,
            Games = games,
            ExpiresAt = DateTime.UtcNow.AddMinutes(SessionDurationMinutes)
        };

        _cache.Set(GetCacheKey(session.Id), session, _cacheOptions);
        
        return session;
    }

    public VotingSession? GetSessionAsync(string sessionId)
    {
        _cache.TryGetValue(GetCacheKey(sessionId), out VotingSession? session);
        return session;
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
        _cache.Set(GetCacheKey(sessionId), session, _cacheOptions);

        // Send real-time update to all clients in the session
        var results = await GetResultsAsync(sessionId);
        await _hubContext.Clients.Group(sessionId)
            .SendAsync("VotesUpdated", results);
        
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
            ExpiresAt = session.ExpiresAt,
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

    public async Task CleanupExpiredSessionsAsync()
    {
        // This method can be called by a background service to clean up expired sessions
        // However, with the MemoryCache and absolute expiration, cleanup is handled automatically
        await Task.CompletedTask;
    }

    private string GetCacheKey(string sessionId) => $"voting_session_{sessionId}";
} 