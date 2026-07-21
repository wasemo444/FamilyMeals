using ManageFamilyMeals.Api.Data;
using ManageFamilyMeals.Api.Endpoints;
using ManageFamilyMeals.Api.Middleware;
using ManageFamilyMeals.Api.Services;
using ManageFamilyMeals.Shared.Services;
using Microsoft.EntityFrameworkCore;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

var useSqliteForTesting = builder.Environment.IsEnvironment("Testing");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (useSqliteForTesting)
    {
        var sqlitePath = builder.Configuration["Testing:SqlitePath"]
            ?? "Data Source=managefamilymeals-testing.db";
        options.UseSqlite(sqlitePath);
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
});

builder.Services.AddScoped<IAppDataStore, EfAppDataStore>();
builder.Services.AddScoped<IMealDataService, MealDataService>();

builder.Services.AddHttpClient(nameof(LinkPreviewService), client =>
{
    client.Timeout = TimeSpan.FromSeconds(12);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = true,
    AutomaticDecompression = DecompressionMethods.All
});
builder.Services.AddSingleton<LinkPreviewService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy => policy
        .WithOrigins(
            "http://localhost:5084",
            "https://localhost:7039")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (dbContext.Database.IsRelational()
            && dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true)
        {
            dbContext.Database.Migrate();
        }
        else
        {
            dbContext.Database.EnsureCreated();
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(
            ex,
            "Database migration failed. Ensure PostgreSQL is running (docker compose up) and the connection string is valid.");
        throw;
    }
}
else if (app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
}

using (var scope = app.Services.CreateScope())
{
    var mealDataService = scope.ServiceProvider.GetRequiredService<IMealDataService>();
    await mealDataService.RunMaintenanceAsync();
}

app.UseCors("WebClient");
app.UseMiddleware<MealDataLoadMiddleware>();

app.MapBootstrapEndpoints();
app.MapCategoryEndpoints();
app.MapLinkEndpoints();
app.MapSettingsEndpoints();
app.MapLinkPreviewEndpoints();

app.Run();

public partial class Program;
