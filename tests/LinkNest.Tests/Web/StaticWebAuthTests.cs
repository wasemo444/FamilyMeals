using LinkNest.Shared.Auth;
using LinkNest.Shared.Services;
using LinkNest.Web.Client;
using LinkNest.Web.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace LinkNest.Tests.Web;

public class StaticWebClientAuthModeTests
{
    [Fact]
    public void UsesBearerToken_IsTrue()
    {
        var mode = new StaticWebClientAuthMode();
        Assert.True(mode.UsesBearerToken);
    }
}

public class BrowserSecureTokenStoreTests
{
    [Fact]
    public void TryParseExpiry_WithRoundTripValue_ReturnsTrue()
    {
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var raw = expiresAt.ToString("O");

        var parsed = BrowserSecureTokenStore.TryParseExpiry(raw, out var result);

        Assert.True(parsed);
        Assert.Equal(expiresAt, result);
    }

    [Fact]
    public void TryParseExpiry_WithInvalidValue_ReturnsFalse()
    {
        var parsed = BrowserSecureTokenStore.TryParseExpiry("not-a-date", out _);
        Assert.False(parsed);
    }

    [Fact]
    public async Task GetAccessTokenAsync_WhenExpired_ClearsStorageAndReturnsNull()
    {
        var js = new FakeJsRuntime(new Dictionary<string, string?>
        {
            [BrowserSecureTokenStore.AccessTokenKey] = "jwt-token",
            [BrowserSecureTokenStore.ExpiresAtKey] = DateTime.UtcNow.AddMinutes(-5).ToString("O")
        });
        var store = new BrowserSecureTokenStore(js);

        var token = await store.GetAccessTokenAsync();

        Assert.Null(token);
        Assert.False(js.ContainsKey(BrowserSecureTokenStore.AccessTokenKey));
        Assert.False(js.ContainsKey(BrowserSecureTokenStore.ExpiresAtKey));
    }

    [Fact]
    public async Task GetAccessTokenAsync_WhenValid_ReturnsToken()
    {
        var js = new FakeJsRuntime(new Dictionary<string, string?>
        {
            [BrowserSecureTokenStore.AccessTokenKey] = "jwt-token",
            [BrowserSecureTokenStore.ExpiresAtKey] = DateTime.UtcNow.AddHours(1).ToString("O")
        });
        var store = new BrowserSecureTokenStore(js);

        var token = await store.GetAccessTokenAsync();

        Assert.Equal("jwt-token", token);
    }

    [Fact]
    public async Task SaveAsync_PersistsTokenAndExpiry()
    {
        var js = new FakeJsRuntime();
        var store = new BrowserSecureTokenStore(js);
        var expiresAt = DateTime.UtcNow.AddHours(1);

        await store.SaveAsync("saved-token", expiresAt);

        Assert.Equal("saved-token", js.Get(BrowserSecureTokenStore.AccessTokenKey));
        Assert.Equal(expiresAt.ToString("O"), js.Get(BrowserSecureTokenStore.ExpiresAtKey));
    }

    private sealed class FakeJsRuntime : IJSRuntime
    {
        private readonly Dictionary<string, string?> _storage;

        public FakeJsRuntime(Dictionary<string, string?>? storage = null) =>
            _storage = storage ?? new Dictionary<string, string?>(StringComparer.Ordinal);

        public bool ContainsKey(string key) => _storage.ContainsKey(key);

        public string? Get(string key) => _storage.GetValueOrDefault(key);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier == "sessionStorage.setItem" && args is { Length: 2 })
            {
                _storage[args[0]?.ToString() ?? string.Empty] = args[1]?.ToString();
                return ValueTask.FromResult(default(TValue)!);
            }

            if (identifier == "sessionStorage.getItem" && args is { Length: 1 })
            {
                var key = args[0]?.ToString() ?? string.Empty;
                _storage.TryGetValue(key, out var value);
                return ValueTask.FromResult((TValue?)(object?)value!);
            }

            if (identifier == "sessionStorage.removeItem" && args is { Length: 1 })
            {
                _storage.Remove(args[0]?.ToString() ?? string.Empty);
                return ValueTask.FromResult(default(TValue)!);
            }

            throw new NotSupportedException($"Unexpected JS call: {identifier}");
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);
    }
}

public class StaticWebClientServiceRegistrationTests
{
    [Fact]
    public void AddLinkNestStaticWebClientServices_RegistersBearerAuthServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiBaseUrl"] = "http://localhost:5084"
            })
            .Build();

        services.AddTransient<BearerTokenHandler>();
        services.AddLinkNestStaticWebClientServices(configuration);

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IClientAuthMode) && d.ImplementationType == typeof(StaticWebClientAuthMode));
        Assert.Contains(services, d =>
            d.ServiceType == typeof(ISecureTokenStore) && d.ImplementationType == typeof(BrowserSecureTokenStore));
        Assert.Contains(services, d =>
            d.ServiceType == typeof(AuthenticationStateProvider));
        Assert.Contains(services, d =>
            d.ServiceType == typeof(IAuthStateNotifier));
        Assert.Contains(services, d =>
            d.ServiceType == typeof(UnauthorizedSessionHandler));
    }

    [Fact]
    public void AddLinkNestBearerAuth_RegistersJwtProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IClientAuthMode, StaticWebClientAuthMode>();
        services.AddSingleton<ISecureTokenStore, NoOpSecureTokenStore>();
        services.AddSingleton<IAuthClient, NoOpAuthClient>();
        services.AddLinkNestBearerAuth();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.IsType<JwtAuthenticationStateProvider>(scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>());
    }

    private sealed class NoOpSecureTokenStore : ISecureTokenStore
    {
        public Task SaveAsync(string accessToken, DateTime expiresAtUtc, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task ClearAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NoOpAuthClient : IAuthClient
    {
        public Task<AuthUserInfo> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<AuthUserInfo> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<AuthTokenResponse> LoginWithTokenAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task LogoutAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<AuthUserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<AuthUserInfo?>(null);

        public Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task ResendConfirmationAsync(ResendConfirmationRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<AuthUserInfo> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task DeactivateAccountAsync(DeactivateAccountRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
