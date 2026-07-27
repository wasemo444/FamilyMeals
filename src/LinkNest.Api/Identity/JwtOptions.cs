namespace LinkNest.Api.Identity;

/// <summary>
/// Configuration for JWT bearer tokens issued to mobile clients.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>Configuration section name (<c>Jwt</c>).</summary>
    public const string SectionName = "Jwt";

    /// <summary>Symmetric signing key (minimum 32 characters for HS256).</summary>
    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = "LinkNest.Api";

    public string Audience { get; set; } = "LinkNest.Mobile";

    /// <summary>Access token lifetime in minutes.</summary>
    public int ExpirationMinutes { get; set; } = 60 * 24 * 14;
}
