using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using GameRecommender.Services;
using System.Security.Claims;
using GameRecommender.Models;

namespace GameRecommender.Controllers;

public class AuthController : Controller
{
    private readonly ILogger<AuthController> _logger;
    private readonly ISteamAuthService _steamAuth;

    public AuthController(ILogger<AuthController> logger, ISteamAuthService steamAuth)
    {
        _logger = logger;
        _steamAuth = steamAuth;
    }

    [HttpGet("login")]
    public IActionResult Login(string returnUrl = "/")
    {
        _logger.LogInformation("User attempting to login with Steam");
        return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, "Steam");
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // This action will be called by the Steam authentication middleware after successful login
    public async Task<IActionResult> ExternalLoginCallback()
    {
        var result = await HttpContext.AuthenticateAsync("Steam");
        if (!result.Succeeded)
        {
            _logger.LogError("Steam authentication failed");
            return RedirectToAction("Index", "Home");
        }

        if (result.Principal != null)
        {
            var steamIdClaim = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(steamIdClaim))
            {
                _logger.LogError("Steam ID not found in claims");
                return RedirectToAction("Index", "Home");
            }

            // Extract just the numeric SteamID from the claim URL
            var steamId = steamIdClaim?.Replace("https://steamcommunity.com/openid/id/", "");
            if (string.IsNullOrEmpty(steamId))
            {
                _logger.LogError("Invalid Steam ID format");
                return RedirectToAction("Index", "Home");
            }

            // Add this to get the avatar hash from Steam claims
            var avatarHash = result.Principal?.FindFirst("urn:steam:avatar")?.Value ?? string.Empty;

            // Get the existing claims and add our new ones
            var existingClaims = result.Principal.Claims.ToList();
            var steamUser = await _steamAuth.GetUserProfileAsync(steamId);
            var newClaims = new List<Claim>
            {
                new("ProfileUrl", steamUser.ProfileUrl),
                new("AvatarUrl", steamUser.AvatarUrl),
                new(ClaimTypes.Name, steamUser.Name)
            };

            // Combine existing and new claims
            var claimsIdentity = new ClaimsIdentity(existingClaims, "Cookies");
            claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, steamId)); // Override with clean ID

            // Sign in with the combined claims
            await HttpContext.SignInAsync(
                "Cookies",
                new ClaimsPrincipal(claimsIdentity),
                result.Properties);
        }

        return RedirectToAction("Index", "Home");
    }
} 