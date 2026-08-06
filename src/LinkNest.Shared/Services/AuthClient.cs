using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LinkNest.Shared.Auth;

namespace LinkNest.Shared.Services;

/// <summary>
/// HTTP client implementation of <see cref="IAuthClient"/> that maps API error responses
/// to <see cref="Auth.AuthValidationException"/> and <see cref="UnauthorizedAccessException"/>.
/// </summary>
/// <param name="httpClientFactory">Factory for the named <c>LinkNestApi</c> HTTP client.</param>
public sealed class AuthClient(IHttpClientFactory httpClientFactory) : IAuthClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private HttpClient Http => httpClientFactory.CreateClient("LinkNestApi");

    /// <inheritdoc />
    public async Task<AuthUserInfo> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsJsonAsync("/api/auth/register", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadUserAsync(response, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize registered user.");
    }

    /// <inheritdoc />
    public async Task<AuthUserInfo> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsJsonAsync("/api/auth/login", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadUserAsync(response, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize logged-in user.");
    }

    /// <inheritdoc />
    public async Task<AuthTokenResponse> LoginWithTokenAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsJsonAsync("/api/auth/token", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<AuthTokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize token response.");
    }

    /// <inheritdoc />
    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsync("/api/auth/logout", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task<AuthUserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var response = await Http.GetAsync("/api/auth/me", cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadUserAsync(response, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsJsonAsync("/api/auth/forgot-password", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsJsonAsync("/api/auth/reset-password", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ResendConfirmationAsync(ResendConfirmationRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsJsonAsync("/api/auth/resend-confirmation", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsJsonAsync("/api/auth/confirm-email", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AuthUserInfo> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Http.PatchAsJsonAsync("/api/auth/me", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadUserAsync(response, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize updated user.");
    }

    /// <inheritdoc />
    public async Task DeactivateAccountAsync(DeactivateAccountRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsJsonAsync("/api/auth/deactivate", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var body = await ReadResponseBodyAsync(response, cancellationToken);
            var detail = TryReadProblemDetail(body ?? string.Empty)
                ?? "Invalid email or password.";
            throw new UnauthorizedAccessException(detail);
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var lockoutBody = await ReadResponseBodyAsync(response, cancellationToken);
            var lockoutMessage = string.IsNullOrWhiteSpace(lockoutBody)
                ? "Account is temporarily locked. Try again later."
                : TryReadProblemDetail(lockoutBody) ?? "Account is temporarily locked. Try again later.";
            throw new AuthValidationException(new Dictionary<string, string[]>
            {
                ["error"] = [lockoutMessage]
            });
        }

        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            var unavailableBody = await ReadResponseBodyAsync(response, cancellationToken);
            var unavailableMessage = TryReadProblemDetail(unavailableBody ?? string.Empty)
                ?? "Service temporarily unavailable. Please try again.";
            throw new AuthValidationException(new Dictionary<string, string[]>
            {
                ["error"] = [unavailableMessage]
            });
        }

        var responseBody = await ReadResponseBodyAsync(response, cancellationToken);
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            response.EnsureSuccessStatusCode();
            return;
        }

        var validationErrors = TryReadValidationErrors(responseBody);
        if (validationErrors is not null)
        {
            throw new AuthValidationException(validationErrors);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var errorMessage = TryReadErrorMessage(responseBody);
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new AuthValidationException(new Dictionary<string, string[]>
                {
                    ["error"] = [errorMessage]
                });
            }
        }

        response.EnsureSuccessStatusCode();
    }

    private static async Task<string?> ReadResponseBodyAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength == 0)
        {
            return null;
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static IReadOnlyDictionary<string, string[]>? TryReadValidationErrors(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (!document.RootElement.TryGetProperty("errors", out var errorsElement)
                || errorsElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in errorsElement.EnumerateObject())
            {
                errors[property.Name] = property.Value.ValueKind == JsonValueKind.Array
                    ? property.Value.EnumerateArray()
                        .Select(item => item.GetString() ?? string.Empty)
                        .Where(message => !string.IsNullOrWhiteSpace(message))
                        .ToArray()
                    : [property.Value.GetString() ?? string.Empty];
            }

            return errors.Count == 0 ? null : errors;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? TryReadErrorMessage(string responseBody)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody, JsonOptions);
            return payload?.GetValueOrDefault("error");
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? TryReadProblemDetail(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (document.RootElement.TryGetProperty("detail", out var detailElement))
            {
                return detailElement.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static async Task<AuthUserInfo?> ReadUserAsync(HttpResponseMessage response, CancellationToken cancellationToken) =>
        await response.Content.ReadFromJsonAsync<AuthUserInfo>(cancellationToken);
}
