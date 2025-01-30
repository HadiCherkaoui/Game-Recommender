using Microsoft.EntityFrameworkCore;
using GameRecommender.Data;
using GameRecommender.Services;
using GameRecommender.Config;
using GameRecommender.Hubs;
using Microsoft.AspNetCore.DataProtection;

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
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
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
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
    options.SaveTokens = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCookiePolicy();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<VotingHub>("/votingHub");

app.Run(); 