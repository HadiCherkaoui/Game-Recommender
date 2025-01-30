using Microsoft.EntityFrameworkCore;
using GameRecommender.Data;
using GameRecommender.Services;
using GameRecommender.Config;
using GameRecommender.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddHttpClient();
builder.Services.Configure<SteamConfig>(builder.Configuration.GetSection("Steam"));
builder.Services.AddScoped<ISteamAuthService, SteamAuthService>();
builder.Services.AddScoped<SteamStoreService>();
builder.Services.AddScoped<IGameRecommendationService, GameRecommendationService>();
builder.Services.AddScoped<IVotingSessionService, VotingSessionService>();
builder.Services.AddMemoryCache();

// Add database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Steam";
})
.AddCookie("Cookies")
.AddSteam(options =>
{
    options.ApplicationKey = builder.Configuration["Steam:ApiKey"]!;
    options.UserInformationEndpoint = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/";
    options.CallbackPath = "/auth/ExternalLoginCallback";
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
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<VotingHub>("/votingHub");

app.Run(); 