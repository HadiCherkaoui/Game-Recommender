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
}

// Configure static files with explicit options
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "wwwroot")),
    RequestPath = "",
    OnPrepareResponse = ctx =>
    {
        // Add proper CORS headers for the domain
        ctx.Context.Response.Headers.Append(
            "Access-Control-Allow-Origin", "https://app.cherkaoui.ch");
        
        // Set proper content types for common files
        var path = ctx.File.PhysicalPath.ToLower();
        if (path.EndsWith(".css"))
            ctx.Context.Response.ContentType = "text/css";
        else if (path.EndsWith(".js"))
            ctx.Context.Response.ContentType = "application/javascript";
        
        // Disable caching for now
        ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store");
        ctx.Context.Response.Headers.Append("Expires", "-1");
    }
});

app.UseCookiePolicy();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Configure forwarded headers
app.Use((context, next) =>
{
    context.Request.Scheme = "https";
    context.Request.Host = new HostString("app.cherkaoui.ch");
    return next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<VotingHub>("/votingHub");

app.Run(); 