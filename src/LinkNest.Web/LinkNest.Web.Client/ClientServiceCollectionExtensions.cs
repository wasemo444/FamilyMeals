using LinkNest.Shared.Resources;
using LinkNest.Shared.Services;
using LinkNest.Web.Client.Services;
using Microsoft.Extensions.Configuration;

namespace LinkNest.Web.Client;

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
    /// <param name="baseAddress">Fallback base URL for the <c>LinkNestApi</c> client when configuration is absent.</param>
    /// <param name="configureLinkNestApi">Optional callback to add handlers (for example cookie forwarding on the Web host).</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddLinkNestClientServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string? baseAddress = null,
        Action<IHttpClientBuilder>? configureLinkNestApi = null)
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

        var linkNestApiBuilder = services.AddHttpClient("LinkNestApi", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
        });

        configureLinkNestApi?.Invoke(linkNestApiBuilder);

        services.AddScoped<IContentDataService, ApiContentDataService>();
        services.AddScoped<IGroupService, GroupClient>();
        services.AddScoped<IAuthClient, AuthClient>();
        services.AddScoped<CultureService>();
        services.AddScoped<ILinkPreviewClient, LinkPreviewClient>();

        return services;
    }
}
