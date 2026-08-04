using LinkNest.Shared.Services;
using Microsoft.JSInterop;

namespace LinkNest.Web.Client.Services;

/// <summary>
/// Stores JWT access tokens in browser <c>sessionStorage</c> with UTC expiration checks.
/// </summary>
public sealed class BrowserSecureTokenStore(IJSRuntime jsRuntime) : ISecureTokenStore
{
    public const string AccessTokenKey = "linknest.accessToken";
    public const string ExpiresAtKey = "linknest.tokenExpiresAtUtc";

    /// <inheritdoc />
    public async Task SaveAsync(string accessToken, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
    {
        await jsRuntime.InvokeVoidAsync("sessionStorage.setItem", cancellationToken, AccessTokenKey, accessToken);
        await jsRuntime.InvokeVoidAsync("sessionStorage.setItem", cancellationToken, ExpiresAtKey, expiresAtUtc.ToString("O"));
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var token = await jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", cancellationToken, AccessTokenKey);
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var expiresAtRaw = await jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", cancellationToken, ExpiresAtKey);
        if (!TryParseExpiry(expiresAtRaw, out var expiresAtUtc) || expiresAtUtc <= DateTime.UtcNow)
        {
            await ClearAsync(cancellationToken);
            return null;
        }

        return token;
    }

    /// <inheritdoc />
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", cancellationToken, AccessTokenKey);
        await jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", cancellationToken, ExpiresAtKey);
    }

    public static bool TryParseExpiry(string? expiresAtRaw, out DateTime expiresAtUtc)
    {
        expiresAtUtc = default;
        return !string.IsNullOrWhiteSpace(expiresAtRaw)
            && DateTime.TryParse(expiresAtRaw, null, System.Globalization.DateTimeStyles.RoundtripKind, out expiresAtUtc);
    }
}
