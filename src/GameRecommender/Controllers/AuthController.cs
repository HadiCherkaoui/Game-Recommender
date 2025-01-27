using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using GameRecommender.Services;
using System.Security.Claims;

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

        var steamId = result.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(steamId))
        {
            _logger.LogError("Steam ID not found in claims");
            return RedirectToAction("Index", "Home");
        }

        // Get the existing claims and add our new ones
        var existingClaims = result.Principal.Claims.ToList();
        var newClaims = new List<Claim>
        {
            new Claim("ProfileUrl", $"https://steamcommunity.com/profiles/{steamId}"),
            new Claim("AvatarUrl", $"https://avatars.akamai.steamstatic.com/{steamId}.jpg")
        };

        // Combine existing and new claims
        var identity = new ClaimsIdentity(existingClaims.Concat(newClaims), "Steam");
        var principal = new ClaimsPrincipal(identity);

        // Sign in with the combined claims
        await HttpContext.SignInAsync("Cookies", principal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        });

        return RedirectToAction("Index", "Home");
    }
} 