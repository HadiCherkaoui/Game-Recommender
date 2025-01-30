using Microsoft.EntityFrameworkCore;
using GameRecommender.Data;
using GameRecommender.Services;
using GameRecommender.Config;
using GameRecommender.Hubs;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Configure Data Protection
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["DataProtection:Path"] ?? "/data/keys"))
    .SetApplicationName(builder.Configuration["DataProtection:ApplicationName"] ?? "GameRecommender");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddHttpClient();
builder.Services.Configure<SteamConfig>(builder.Configuration.GetSection("Steam"));
builder.Services.AddScoped<ISteamAuthService, SteamAuthService>();
builder.Services.AddScoped<SteamStoreService>();
builder.Services.AddScoped<IGameRecommendationService, GameRecommendationService>();
builder.Services.AddSingleton<IVotingSessionService, VotingSessionService>();
builder.Services.AddMemoryCache();

// Configure static files
builder.Services.Configure<StaticFileOptions>(options =>
{
    options.ServeUnknownFileTypes = true;
    options.DefaultContentType = "application/octet-stream";
});

// Add database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure cookie policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => false;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Steam";
})
.AddCookie("Cookies", options =>
{
    options.Cookie.Name = ".GameRecommender.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
})
.AddSteam(options =>
{
    options.ApplicationKey = builder.Configuration["Steam:ApiKey"]!;
    options.UserInformationEndpoint = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/";
    options.CallbackPath = "/auth/ExternalLoginCallback";
    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.None;
    options.SaveTokens = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    
    // Production static files configuration
    var wwwrootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    Console.WriteLine($"wwwroot path: {wwwrootPath}");
    Console.WriteLine($"Directory exists: {Directory.Exists(wwwrootPath)}");
    if (Directory.Exists(wwwrootPath))
    {
        foreach (var file in Directory.GetFiles(wwwrootPath, "*.*", SearchOption.AllDirectories))
        {
            Console.WriteLine($"Found file: {file}");
        }
    }
    
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(wwwrootPath),
        RequestPath = "",
        OnPrepareResponse = ctx =>
        {
            Console.WriteLine($"Serving static file: {ctx.File.PhysicalPath}");
            ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store");
            ctx.Context.Response.Headers.Append("Expires", "-1");
        }
    });
}
else
{
    app.UseStaticFiles();
}

app.UseCookiePolicy();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<VotingHub>("/votingHub");

app.Run(); 