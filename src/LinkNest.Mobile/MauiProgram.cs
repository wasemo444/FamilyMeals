using LinkNest.Shared.Auth;
using LinkNest.Shared.Configuration;
using LinkNest.Web.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LinkNest.Mobile;

/// <summary>
/// MAUI application entry point and dependency injection configuration.
/// </summary>
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        InteractiveRenderSettings.ConfigureBlazorHybridRenderModes();

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Configuration
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true);

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ApiBaseUrl"] = MobileApiConfiguration.ResolveApiBaseUrl(builder.Configuration)
        });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddAuthorizationCore();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddTransient<BearerTokenHandler>();
        builder.Services.AddLinkNestMobileClientServices(builder.Configuration);

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
