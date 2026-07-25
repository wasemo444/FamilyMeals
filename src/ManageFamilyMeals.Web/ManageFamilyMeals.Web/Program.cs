using ManageFamilyMeals.Api.Data;
using ManageFamilyMeals.Api.Identity;
using ManageFamilyMeals.Api.Startup;
using ManageFamilyMeals.Web.Auth;
using ManageFamilyMeals.Web.Client;
using ManageFamilyMeals.Web.Components;
using ManageFamilyMeals.Shared.Services;
using ManageFamilyMeals.Web.Endpoints;
using ManageFamilyMeals.Web.ReverseProxy;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

var useSqliteForTesting = builder.Environment.IsEnvironment("Testing");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (useSqliteForTesting)
    {
        var sqlitePath = builder.Configuration["Testing:SqlitePath"]
            ?? "Data Source=managefamilymeals-web-testing.db";
        options.UseSqlite(sqlitePath);
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
});

builder.Services.AddManageFamilyMealsIdentity(builder.Configuration, builder.Environment);
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();

builder.Services.AddLocalization();
builder.Services.AddMealDataApiProxy();
builder.Services.AddTransient<CookieForwardingHandler>();
builder.Services.AddManageFamilyMealsClientServices(
    builder.Configuration,
    builder.Configuration["WebBaseUrl"] ?? "http://localhost:5084/",
    mealDataApi => mealDataApi.AddHttpMessageHandler<CookieForwardingHandler>());
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthClient>();
builder.Services.AddScoped<IAuthClient, WebHostAuthClient>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization(options => options.SerializeAllClaims = true);

var app = builder.Build();

if ((app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
    && !app.Configuration.GetValue<bool>("Database:SkipInitialization"))
{
    await app.InitializeDatabaseAsync();
}

var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("ar") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api")
        && !context.Request.Path.StartsWithSegments("/account"),
    builder => builder.UseAntiforgery());

app.MapMealDataApiProxy();
app.MapAccountEndpoints();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(ManageFamilyMeals.Web.Client._Imports).Assembly);

app.Run();

public partial class Program;
