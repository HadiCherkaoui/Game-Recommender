using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameRecommender.Data;
using GameRecommender.Models;

namespace GameRecommender.Controllers;

public class GamesController : Controller
{
    private readonly ApplicationDbContext _context;

    public GamesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.Games.ToListAsync());
    }
} 