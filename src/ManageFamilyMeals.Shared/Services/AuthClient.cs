using System.Net;
using System.Net.Http.Json;
using ManageFamilyMeals.Shared.Auth;

namespace ManageFamilyMeals.Shared.Services;

public sealed class AuthClient(IHttpClientFactory httpClientFactory) : IAuthClient
{
    private HttpClient Http => httpClientFactory.CreateClient("MealDataApi");

    public async Task<AuthUserInfo> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsJsonAsync("/api/auth/register", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadUserAsync(response, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize registered user.");
    }

    public async Task<AuthUserInfo> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsJsonAsync("/api/auth/login", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadUserAsync(response, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize logged-in user.");
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsync("/api/auth/logout", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<AuthUserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var response = await Http.GetAsync("/api/auth/me", cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await ReadUserAsync(response, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        response.EnsureSuccessStatusCode();
    }

    private static async Task<AuthUserInfo?> ReadUserAsync(HttpResponseMessage response, CancellationToken cancellationToken) =>
        await response.Content.ReadFromJsonAsync<AuthUserInfo>(cancellationToken);
}
