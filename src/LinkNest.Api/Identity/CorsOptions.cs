namespace LinkNest.Api.Identity;

/// <summary>
/// Configuration options for cross-origin requests bound from the <c>Cors</c> configuration section.
/// </summary>
public sealed class CorsOptions
{
    /// <summary>Configuration section name (<c>Cors</c>).</summary>
    public const string SectionName = "Cors";

    /// <summary>Comma-separated list of allowed origins for production static web clients.</summary>
    public string AllowedOrigins { get; set; } = string.Empty;

    /// <summary>Parses <see cref="AllowedOrigins"/> into a trimmed, non-empty array.</summary>
    public string[] GetAllowedOrigins() =>
        AllowedOrigins
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
