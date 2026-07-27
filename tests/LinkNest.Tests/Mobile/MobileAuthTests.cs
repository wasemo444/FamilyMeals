using System.Net;
using LinkNest.Shared.Auth;
using LinkNest.Shared.Services;
using Microsoft.Extensions.Configuration;

namespace LinkNest.Tests.Mobile;

public class BearerTokenHandlerTests
{
    [Fact]
    public async Task SendAsync_WithStoredToken_AddsBearerAuthorizationHeader()
    {
        // Arrange
        var store = new FakeSecureTokenStore { AccessToken = "test-jwt-token" };
        var handler = new BearerTokenHandler(store)
        {
            InnerHandler = new StubHttpMessageHandler(HttpStatusCode.OK)
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };

        // Act
        await client.GetAsync("/api/auth/me");

        // Assert
        Assert.NotNull(StubHttpMessageHandler.LastRequest);
        Assert.Equal("Bearer", StubHttpMessageHandler.LastRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("test-jwt-token", StubHttpMessageHandler.LastRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task SendAsync_WithoutStoredToken_DoesNotAddAuthorizationHeader()
    {
        // Arrange
        var store = new FakeSecureTokenStore();
        var handler = new BearerTokenHandler(store)
        {
            InnerHandler = new StubHttpMessageHandler(HttpStatusCode.OK)
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };

        // Act
        await client.GetAsync("/api/auth/me");

        // Assert
        Assert.NotNull(StubHttpMessageHandler.LastRequest);
        Assert.Null(StubHttpMessageHandler.LastRequest!.Headers.Authorization);
    }

    private sealed class FakeSecureTokenStore : ISecureTokenStore
    {
        public string? AccessToken { get; set; }

        public Task SaveAsync(string accessToken, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
        {
            AccessToken = accessToken;
            return Task.CompletedTask;
        }

        public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(AccessToken);

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            AccessToken = null;
            return Task.CompletedTask;
        }
    }

    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public static HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }
}

public class JwtAuthenticationStateProviderTests
{
    [Fact]
    public async Task GetAuthenticationStateAsync_WhenUserIsNull_ClearsStoredToken()
    {
        // Arrange
        var store = new TrackingSecureTokenStore { AccessToken = "expired-token" };
        var authClient = new FakeAuthClient { CurrentUser = null };
        var provider = new JwtAuthenticationStateProvider(store, authClient);

        // Act
        var state = await provider.GetAuthenticationStateAsync();

        // Assert
        Assert.False(state.User.Identity?.IsAuthenticated ?? false);
        Assert.True(store.WasCleared);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_OnNetworkError_DoesNotClearStoredToken()
    {
        // Arrange
        var store = new TrackingSecureTokenStore { AccessToken = "valid-token" };
        var authClient = new FakeAuthClient { ThrowNetworkError = true };
        var provider = new JwtAuthenticationStateProvider(store, authClient);

        // Act
        var state = await provider.GetAuthenticationStateAsync();

        // Assert
        Assert.False(state.User.Identity?.IsAuthenticated ?? false);
        Assert.False(store.WasCleared);
        Assert.Equal("valid-token", store.AccessToken);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_WithValidUser_ReturnsAuthenticatedState()
    {
        // Arrange
        var user = new AuthUserInfo
        {
            Id = Guid.NewGuid(),
            Email = "dev@linknest.local",
            DisplayName = "Dev User"
        };
        var store = new TrackingSecureTokenStore { AccessToken = "valid-token" };
        var authClient = new FakeAuthClient { CurrentUser = user };
        var provider = new JwtAuthenticationStateProvider(store, authClient);

        // Act
        var state = await provider.GetAuthenticationStateAsync();

        // Assert
        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.Equal(user.Email, state.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value);
    }

    private sealed class TrackingSecureTokenStore : ISecureTokenStore
    {
        public string? AccessToken { get; set; }
        public bool WasCleared { get; private set; }

        public Task SaveAsync(string accessToken, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
        {
            AccessToken = accessToken;
            return Task.CompletedTask;
        }

        public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(AccessToken);

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            WasCleared = true;
            AccessToken = null;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAuthClient : IAuthClient
    {
        public AuthUserInfo? CurrentUser { get; set; }
        public bool ThrowNetworkError { get; set; }

        public Task<AuthUserInfo> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<AuthUserInfo> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<AuthTokenResponse> LoginWithTokenAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task LogoutAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<AuthUserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            if (ThrowNetworkError)
            {
                throw new HttpRequestException("Network unreachable");
            }

            return Task.FromResult(CurrentUser);
        }
    }
}

public class MobileApiConfigurationTests
{
    [Fact]
    public void ResolveApiBaseUrl_UsesEnvironmentVariableWhenConfigurationIsEmpty()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        Environment.SetEnvironmentVariable("LINKNEST_API_BASE_URL", "http://192.168.1.10:5280");

        try
        {
            // Act
            var resolved = LinkNest.Shared.Configuration.MobileApiConfiguration.ResolveApiBaseUrl(configuration);

            // Assert
            Assert.Equal("http://192.168.1.10:5280/", resolved);
        }
        finally
        {
            Environment.SetEnvironmentVariable("LINKNEST_API_BASE_URL", null);
        }
    }

    [Fact]
    public void ResolveApiBaseUrl_NormalizesTrailingSlash()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiBaseUrl"] = "http://localhost:5280"
            })
            .Build();

        // Act
        var resolved = LinkNest.Shared.Configuration.MobileApiConfiguration.ResolveApiBaseUrl(configuration);

        // Assert
        Assert.Equal("http://localhost:5280/", resolved);
    }
}
