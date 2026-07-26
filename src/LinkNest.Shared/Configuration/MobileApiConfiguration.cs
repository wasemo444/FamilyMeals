using Microsoft.Extensions.Configuration;

namespace LinkNest.Shared.Configuration;

/// <summary>
/// Resolves the LinkNest API base URL for mobile clients.
/// </summary>
public static class MobileApiConfiguration
{
    /// <summary>
    /// Priority: configuration <c>ApiBaseUrl</c>, then <c>LINKNEST_API_BASE_URL</c>, then platform default.
    /// </summary>
    public static string ResolveApiBaseUrl(IConfiguration configuration)
    {
        var fromConfig = configuration["ApiBaseUrl"];
        if (!string.IsNullOrWhiteSpace(fromConfig))
        {
            return NormalizeBaseUrl(fromConfig);
        }

        var fromEnvironment = Environment.GetEnvironmentVariable("LINKNEST_API_BASE_URL");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return NormalizeBaseUrl(fromEnvironment);
        }

#if ANDROID
        return "http://10.0.2.2:5280/";
#else
        return "http://localhost:5280/";
#endif
    }

    private static string NormalizeBaseUrl(string baseUrl) =>
        baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
}
