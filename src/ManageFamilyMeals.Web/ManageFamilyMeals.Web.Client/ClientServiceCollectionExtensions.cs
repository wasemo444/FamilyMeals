using ManageFamilyMeals.Shared.Resources;
using ManageFamilyMeals.Shared.Services;
using ManageFamilyMeals.Web.Client.Services;
using Microsoft.Extensions.Configuration;

namespace ManageFamilyMeals.Web.Client;

public static class ClientServiceCollectionExtensions
{
    public static IServiceCollection AddManageFamilyMealsClientServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string? baseAddress = null)
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

        services.AddHttpClient("MealDataApi", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
        });

        services.AddScoped<IMealDataService, ApiMealDataService>();
        services.AddScoped<IAuthClient, AuthClient>();
        services.AddScoped<CultureService>();
        services.AddScoped<ILinkPreviewClient, LinkPreviewClient>();

        return services;
    }
}
