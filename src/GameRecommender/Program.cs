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
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.None : CookieSecurePolicy.Always;
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
    options.CorrelationCookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.None : CookieSecurePolicy.Always;
    options.SaveTokens = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
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
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
        if (allowedOrigins?.Length > 0)
        {
            ctx.Context.Response.Headers.Append(
                "Access-Control-Allow-Origin", string.Join(",", allowedOrigins));
        }
        
        // Set proper content types for common files
        var path = ctx.File.PhysicalPath?.ToLower();
        if (path?.EndsWith(".css") == true)
            ctx.Context.Response.ContentType = "text/css";
        else if (path?.EndsWith(".js") == true)
            ctx.Context.Response.ContentType = "application/javascript";
        
        // Set proper caching for static files
        if (!app.Environment.IsDevelopment())
        {
            ctx.Context.Response.Headers.Append(
                "Cache-Control", "public,max-age=31536000");
        }
        else
        {
            // Disable caching in development
            ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store");
            ctx.Context.Response.Headers.Append("Expires", "-1");
        }
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