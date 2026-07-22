using ManageFamilyMeals.Api.Data;
using ManageFamilyMeals.Api.Identity;
using ManageFamilyMeals.Api.Startup;
using ManageFamilyMeals.Web.Auth;
using ManageFamilyMeals.Web.Client;
using ManageFamilyMeals.Web.Components;
using ManageFamilyMeals.Web.ReverseProxy;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddManageFamilyMealsIdentity(builder.Configuration, builder.Environment);
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();

builder.Services.AddLocalization();
builder.Services.AddMealDataApiProxy(builder.Configuration);
builder.Services.AddManageFamilyMealsClientServices(
    builder.Configuration,
    builder.Configuration["WebBaseUrl"] ?? "http://localhost:5084/");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization(options => options.SerializeAllClaims = true);

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
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
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapReverseProxy();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(ManageFamilyMeals.Web.Client._Imports).Assembly);

app.Run();
