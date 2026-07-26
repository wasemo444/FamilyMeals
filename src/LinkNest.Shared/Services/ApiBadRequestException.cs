namespace LinkNest.Shared.Services;

/// <summary>
/// Thrown when the API returns HTTP 400 with a structured error code the client can map to localized text.
/// </summary>
public sealed class ApiBadRequestException(string code, string? message = null) : Exception(message ?? code)
{
    public string Code { get; } = code;
}
