using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameRecommender.Data;
using GameRecommender.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GameRecommender.Services;
using GameRecommender.Models.ViewModels;
using Microsoft.Extensions.Logging;
using GameRecommender.Extensions;

namespace GameRecommender.Controllers;

public class GamesController : Controller
{
    private readonly ISteamAuthService _steamAuth;
    private readonly ILogger<GamesController> _logger;
    private readonly IGameRecommendationService _recommendationService;
    private readonly IVotingSessionService _votingService;

    public GamesController(
        ISteamAuthService steamAuth, 
        ILogger<GamesController> logger,
        IGameRecommendationService recommendationService,
        IVotingSessionService votingService)
    {
        _steamAuth = steamAuth;
        _logger = logger;
        _recommendationService = recommendationService;
        _votingService = votingService;
    }

    public async Task<IActionResult> Index()
    {
        var steamId = User.GetSteamId();
        
        if (string.IsNullOrEmpty(steamId) || !ulong.TryParse(steamId, out _))
        {
            _logger.LogWarning("Invalid SteamID format");
            return View(new GameListViewModel
            {
                SteamGames = new List<SteamGame>(),
                DebugInfo = "Invalid SteamID format"
            });
        }

        var steamGames = await _steamAuth.GetUserGamesAsync(steamId);
        var viewModel = new GameListViewModel
        {
            SteamGames = steamGames,
            DebugInfo = $"SteamID: {steamId}"
        };
        
        return View(viewModel);
    }

    [Authorize]
    public async Task<IActionResult> ImportFromSteam()
    {
        var steamId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(steamId) || !ulong.TryParse(steamId, out _))
        {
            return RedirectToAction("Index");
        }

        var steamGames = await _steamAuth.GetUserGamesAsync(steamId);
        return View("ImportedGames", steamGames);
    }

    [Authorize]
    public IActionResult StartRecommendation()
    {
        var question = _recommendationService.GetInitialQuestion();
        return View("Recommendation", question);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ProcessAnswer(string questionId, string answerId)
    {
        // Get the current question and selected answer
        var currentQuestion = _recommendationService.GetQuestion(questionId);
        var selectedAnswer = currentQuestion.Answers.First(a => a.Value == answerId);
        
        // Store the answer criteria in TempData
        foreach (var update in selectedAnswer.CriteriaUpdates)
        {
            TempData[$"criteria_{update.Key}"] = update.Value;
        }

        var nextQuestion = _recommendationService.GetNextQuestion(questionId, answerId);
        if (nextQuestion == null)
        {
            // No more questions, show recommendations
            return RedirectToAction("ShowRecommendations");
        }

        return View("Recommendation", nextQuestion);
    }

    [Authorize]
    public async Task<IActionResult> ShowRecommendations()
    {
        var steamId = User.GetSteamId();
        if (string.IsNullOrEmpty(steamId))
        {
            return RedirectToAction("Index");
        }

        var criteria = new GameRecommendationCriteria
        {
            PlayerCount = TempData["criteria_PlayerCount"]?.ToString() is string pc && int.TryParse(pc, out var count) 
                ? count : null,
            WantMultiplayer = TempData["criteria_WantMultiplayer"]?.ToString() is string mp && bool.TryParse(mp, out var multi) 
                ? multi : null,
            WantCoop = TempData["criteria_WantCoop"]?.ToString() is string coop && bool.TryParse(coop, out var cp) 
                ? cp : null,
            PreferredGenre = TempData["criteria_PreferredGenre"]?.ToString(),
            WantStoryRich = TempData["criteria_WantStoryRich"]?.ToString() is string sr && bool.TryParse(sr, out var story) 
                ? story : null,
            GameMood = TempData["criteria_GameMood"]?.ToString()
        };

        var allGames = await _steamAuth.GetUserGamesAsync(steamId);
        var recommendations = _recommendationService.GetRecommendations(allGames, criteria);

        ViewBag.CanCreateVotingSession = true;
        return View(recommendations);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateVotingSession(List<int> selectedGames)
    {
        var steamId = User.GetSteamId();
        if (string.IsNullOrEmpty(steamId))
        {
            return RedirectToAction("Index");
        }

        var allGames = await _steamAuth.GetUserGamesAsync(steamId);
        var gamesForVoting = allGames.Where(g => selectedGames.Contains(g.AppId)).ToList();
        
        var session = await _votingService.CreateSessionAsync(steamId, gamesForVoting);
        
        return RedirectToAction("VotingSession", new { id = session.Id });
    }

    public async Task<IActionResult> VotingSession(string id)
    {
        var session = await _votingService.GetSessionAsync(id);
        if (session == null)
        {
            return NotFound();
        }

        return View(session);
    }

    [HttpPost]
    public async Task<IActionResult> SubmitVotes(string sessionId, string voterName, Dictionary<int, int> ratings)
    {
        var voterId = User.Identity?.IsAuthenticated == true ? 
            User.GetSteamId() : 
            $"anon_{Guid.NewGuid():N}";

        foreach (var rating in ratings)
        {
            var vote = new GameVote
            {
                SessionId = sessionId,
                VoterId = voterId,
                VoterName = voterName,
                GameAppId = rating.Key,
                Rating = rating.Value
            };

            await _votingService.AddVoteAsync(sessionId, vote);
        }

        return RedirectToAction("VotingResults", new { id = sessionId });
    }

    public async Task<IActionResult> VotingResults(string id)
    {
        var results = await _votingService.GetResultsAsync(id);
        if (results == null)
        {
            return NotFound();
        }

        return View(results);
    }
} 