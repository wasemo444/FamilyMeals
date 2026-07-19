using ManageFamilyMeals.Shared.Resources;
using ManageFamilyMeals.Shared.Services;
using ManageFamilyMeals.Web.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace ManageFamilyMeals.Web.Client;

public static class ClientServiceCollectionExtensions
{
    public static IServiceCollection AddManageFamilyMealsClientServices(
        this IServiceCollection services,
        Action<IServiceProvider, HttpClient>? configureHttpClient = null)
    {
        services.AddLocalization();
        services.AddSingleton<CultureState>();
        services.AddSingleton<ILocalizedText, LocalizedText>();
        services.AddScoped<IAppDataStore, LocalStorageAppDataStore>();
        services.AddScoped<IMealDataService, MealDataService>();
        services.AddScoped<CultureService>();
        services.AddScoped<ILinkPreviewClient, LinkPreviewClient>();
        services.AddScoped(sp =>
        {
            var client = new HttpClient();
            if (configureHttpClient is not null)
            {
                configureHttpClient(sp, client);
            }
            else if (sp.GetService<NavigationManager>() is { } navigationManager)
            {
                client.BaseAddress = new Uri(navigationManager.BaseUri);
            }

            return client;
        });

        return services;
    }
}
