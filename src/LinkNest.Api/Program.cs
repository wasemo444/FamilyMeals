using LinkNest.Api.V1Import;
using LinkNest.Api.Data;
using LinkNest.Api.Endpoints;
using LinkNest.Api.Identity;
using LinkNest.Api.Middleware;
using LinkNest.Api.Services;
using LinkNest.Api.Startup;
using LinkNest.Shared.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

var useSqliteForTesting = builder.Environment.IsEnvironment("Testing");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (useSqliteForTesting)
    {
        var sqlitePath = builder.Configuration["Testing:SqlitePath"]
            ?? "Data Source=linknest-testing.db";
        options.UseSqlite(sqlitePath);
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
});

builder.Services.AddLinkNestIdentity(builder.Configuration, builder.Environment);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, HttpContextCurrentUserContext>();
builder.Services.AddScoped<IOwnershipAuthorization, OwnershipAuthorizationService>();
builder.Services.AddScoped<ArchiveMaintenanceService>();
builder.Services.AddScoped<OwnershipBackfillService>();
builder.Services.AddScoped<GroupMembershipService>();
builder.Services.AddExceptionHandler<ConcurrencyConflictExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = useSqliteForTesting ? 1000 : 20;
        limiterOptions.QueueLimit = 0;
    });
});

builder.Services.AddScoped<V1AppDataImportService>();
builder.Services.AddScoped<IAppDataStore, EfAppDataStore>();
builder.Services.AddScoped<IContentDataService, ContentDataService>();

builder.Services.AddSingleton<ISafeUrlValidator, SafeUrlValidator>();
builder.Services.AddSingleton<ISafeUrlFetcher, SafeUrlFetcher>();
builder.Services.AddHttpClient(nameof(SafeUrlFetcher), client =>
{
    client.Timeout = TimeSpan.FromSeconds(12);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
})
.ConfigurePrimaryHttpMessageHandler(sp => new SafeRedirectHttpMessageHandler(
    sp.GetRequiredService<ISafeUrlValidator>(),
    new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.All,
        ConnectCallback = SafeConnectionPinning.CreateConnectCallback()
    }));
builder.Services.AddSingleton<LinkPreviewService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy => policy
        .WithOrigins(
            "http://localhost:5084",
            "https://localhost:7039")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

if (!app.Configuration.GetValue<bool>("Database:SkipInitialization"))
{
    await app.InitializeDatabaseAsync();
}

app.UseCors("WebClient");
app.UseRateLimiter();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ContentDataLoadMiddleware>();

app.MapAuthEndpoints();
app.MapBootstrapEndpoints();
app.MapGroupEndpoints();
app.MapGroupMembershipEndpoints();
app.MapCategoryEndpoints();
app.MapLinkEndpoints();
app.MapSettingsEndpoints();
app.MapLinkPreviewEndpoints();

var importExitCode = await V1ImportCommandRunner.TryRunAsync(args, app);
if (importExitCode is int code)
{
    Environment.ExitCode = code;
    return;
}

app.Run();

/// <summary>Marker type for WebApplicationFactory and integration test host discovery.</summary>
public partial class Program;
