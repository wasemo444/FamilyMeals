using LinkNest.Api.Data;
using LinkNest.Api.Identity;
using LinkNest.Shared.Configuration;
using LinkNest.Api.Services;
using LinkNest.Api.Startup;
using LinkNest.Web.Auth;
using LinkNest.Web.Client;
using LinkNest.Web.Components;
using LinkNest.Shared.Services;
using LinkNest.Web.Endpoints;
using LinkNest.Web.ReverseProxy;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = ConnectionStringNormalizer.Normalize(
    builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured."));

var useSqliteForTesting = builder.Environment.IsEnvironment("Testing");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (useSqliteForTesting)
    {
        var sqlitePath = builder.Configuration["Testing:SqlitePath"]
            ?? "Data Source=linknest-web-testing.db";
        options.UseSqlite(sqlitePath);
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
});

builder.Services.AddLinkNestIdentity(builder.Configuration, builder.Environment, enableOutboundEmail: false);
builder.Services.AddScoped<ArchiveMaintenanceService>();
builder.Services.AddScoped<OwnershipBackfillService>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();

builder.Services.AddLocalization();
builder.Services.AddLinkNestApiProxy();
builder.Services.AddTransient<CookieForwardingHandler>();
builder.Services.AddLinkNestClientServices(
    builder.Configuration,
    builder.Configuration["WebBaseUrl"] ?? "http://localhost:5084/",
    linkNestApi => linkNestApi.AddHttpMessageHandler<CookieForwardingHandler>());
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthClient>();
builder.Services.AddScoped<IAuthClient, WebHostAuthClient>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

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

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseForwardedHeaders();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
var webBaseUrl = app.Configuration["WebBaseUrl"] ?? app.Configuration["Auth:WebBaseUrl"] ?? string.Empty;
if (!app.Environment.IsDevelopment()
    && !app.Environment.IsEnvironment("Testing")
    && webBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api")
        && !context.Request.Path.StartsWithSegments("/account"),
    builder => builder.UseAntiforgery());

app.MapLinkNestApiProxy();
app.MapAccountEndpoints();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(LinkNest.Web.Client._Imports).Assembly);

app.Run();

/// <summary>
/// Entry-point type exposed for integration and E2E test hosts.
/// </summary>
public partial class Program;
