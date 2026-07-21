using ManageFamilyMeals.Shared.Resources;
using ManageFamilyMeals.Shared.Services;
using ManageFamilyMeals.Web.Client.Services;
using Microsoft.Extensions.Configuration;

namespace ManageFamilyMeals.Web.Client;

public static class ClientServiceCollectionExtensions
{
    public static IServiceCollection AddManageFamilyMealsClientServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddLocalization();
        services.AddSingleton<CultureState>();
        services.AddSingleton<ILocalizedText, LocalizedText>();

        var apiBaseUrl = configuration["ApiBaseUrl"] ?? "http://localhost:5280";

        services.AddHttpClient("MealDataApi", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        });

        services.AddScoped<IMealDataService, ApiMealDataService>();
        services.AddScoped<CultureService>();
        services.AddScoped<ILinkPreviewClient, LinkPreviewClient>();

        return services;
    }
}
