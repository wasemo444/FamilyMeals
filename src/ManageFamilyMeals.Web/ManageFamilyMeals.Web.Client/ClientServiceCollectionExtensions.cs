using ManageFamilyMeals.Shared.Resources;
using ManageFamilyMeals.Shared.Services;
using ManageFamilyMeals.Web.Client.Services;
using Microsoft.Extensions.Configuration;

namespace ManageFamilyMeals.Web.Client;

/// <summary>
/// Registers shared client services for Blazor WebAssembly and the unified Web host.
/// </summary>
public static class ClientServiceCollectionExtensions
{
    /// <summary>
    /// Adds localization, HTTP clients, and scoped services used by interactive client components.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">Application configuration (reads <c>ApiBaseUrl</c> when set).</param>
    /// <param name="baseAddress">Fallback base URL for the <c>MealDataApi</c> client when configuration is absent.</param>
    /// <param name="configureMealDataApi">Optional callback to add handlers (for example cookie forwarding on the Web host).</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddManageFamilyMealsClientServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string? baseAddress = null,
        Action<IHttpClientBuilder>? configureMealDataApi = null)
    {
        services.AddLocalization();
        services.AddSingleton<CultureState>();
        services.AddSingleton<ILocalizedText, LocalizedText>();

        var configuredBaseUrl = configuration["ApiBaseUrl"];
        var apiBaseUrl = string.IsNullOrWhiteSpace(configuredBaseUrl)
            ? baseAddress ?? "http://localhost:5084"
            : configuredBaseUrl;

        if (!apiBaseUrl.EndsWith('/'))
        {
            apiBaseUrl += "/";
        }

        var mealDataApiBuilder = services.AddHttpClient("MealDataApi", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
        });

        configureMealDataApi?.Invoke(mealDataApiBuilder);

        services.AddScoped<IMealDataService, ApiMealDataService>();
        services.AddScoped<IGroupService, GroupClient>();
        services.AddScoped<IAuthClient, AuthClient>();
        services.AddScoped<CultureService>();
        services.AddScoped<ILinkPreviewClient, LinkPreviewClient>();

        return services;
    }
}
