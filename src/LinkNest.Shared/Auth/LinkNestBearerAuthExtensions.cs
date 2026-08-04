using LinkNest.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace LinkNest.Shared.Auth;

/// <summary>
/// Shared JWT bearer authentication registration for MAUI and static Blazor WASM clients.
/// </summary>
public static class LinkNestBearerAuthExtensions
{
    /// <summary>
    /// Registers JWT bearer auth services. Callers must register <see cref="IClientAuthMode"/>
    /// and <see cref="ISecureTokenStore"/> before invoking this method.
    /// </summary>
    public static IServiceCollection AddLinkNestBearerAuth(this IServiceCollection services)
    {
        services.AddTransient<UnauthorizedSessionHandler>();
        services.AddScoped<JwtAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<JwtAuthenticationStateProvider>());
        services.AddScoped<IAuthStateNotifier>(sp =>
            sp.GetRequiredService<JwtAuthenticationStateProvider>());
        return services;
    }
}
