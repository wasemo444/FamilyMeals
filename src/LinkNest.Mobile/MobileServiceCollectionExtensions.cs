using LinkNest.Mobile.Services;
using LinkNest.Shared.Auth;
using LinkNest.Shared.Services;
using LinkNest.Web.Client;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;

namespace LinkNest.Mobile;

/// <summary>
/// MAUI-specific dependency injection extensions for bearer-token authentication.
/// </summary>
public static class MobileServiceCollectionExtensions
{
    /// <summary>
    /// Registers JWT bearer auth services for the MAUI Blazor Hybrid client.
    /// </summary>
    public static IServiceCollection AddLinkNestMobileBearerAuth(this IServiceCollection services)
    {
        services.AddSingleton<IClientAuthMode, MobileClientAuthMode>();
        services.AddSingleton<ISecureTokenStore, MauiSecureTokenStore>();
        services.AddTransient<UnauthorizedSessionHandler>();
        services.AddScoped<JwtAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<JwtAuthenticationStateProvider>());
        services.AddScoped<IAuthStateNotifier>(sp =>
            sp.GetRequiredService<JwtAuthenticationStateProvider>());
        return services;
    }

    /// <summary>
    /// Registers core client services and mobile bearer auth in one call.
    /// </summary>
    public static IServiceCollection AddLinkNestMobileClientServices(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IHttpClientBuilder>? configureLinkNestApi = null)
    {
        services.AddLinkNestCoreClientServices(
            configuration,
            configuration["ApiBaseUrl"],
            linkNestApi =>
            {
                linkNestApi.AddHttpMessageHandler<BearerTokenHandler>();
                linkNestApi.AddHttpMessageHandler<UnauthorizedSessionHandler>();
                configureLinkNestApi?.Invoke(linkNestApi);
            });

        services.AddLinkNestMobileBearerAuth();
        return services;
    }
}
