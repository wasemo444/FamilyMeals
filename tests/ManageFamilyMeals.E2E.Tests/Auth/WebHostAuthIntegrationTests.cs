using System.Net;
using System.Net.Http.Json;
using ManageFamilyMeals.Api.Identity;
using ManageFamilyMeals.E2E.Tests.Fixtures;
using ManageFamilyMeals.Shared.Auth;
using ManageFamilyMeals.Shared.Constants;
using ManageFamilyMeals.Tests.Api;

namespace ManageFamilyMeals.E2E.Tests.Auth;

[Collection("E2E")]
public class WebHostAuthIntegrationTests(FullStackApplicationFixture fixture)
{
    private HttpClient CreateWebClient()
    {
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new System.Net.CookieContainer()
        };

        return new HttpClient(handler)
        {
            BaseAddress = new Uri($"{fixture.WebBaseUrl}/")
        };
    }

    [Fact]
    public async Task Login_DirectToApiHost_ReturnsSuccess()
    {
        // Arrange
        using var client = new HttpClient
        {
            BaseAddress = new Uri($"{fixture.ApiFactory.ServerAddress}/")
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = WellKnownUsers.DefaultUserEmail,
            Password = ApiWebApplicationFactory.DefaultTestPassword
        });

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Login_ThroughWebProxy_AllowsAuthenticatedBootstrap()
    {
        // Arrange
        using var client = CreateWebClient();

        // Act
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = WellKnownUsers.DefaultUserEmail,
            Password = ApiWebApplicationFactory.DefaultTestPassword
        });
        var bootstrapResponse = await client.GetAsync("/api/bootstrap");

        // Assert
        loginResponse.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, bootstrapResponse.StatusCode);
    }

    [Fact]
    public async Task Register_ThroughWebProxy_CreatesUserWithoutSigningIn()
    {
        // Arrange
        var email = $"web-{Guid.NewGuid():N}@example.com";
        using var client = CreateWebClient();

        // Act
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "RegisterPass1!",
            ConfirmPassword = "RegisterPass1!",
            DisplayName = "Web Host User"
        });
        var meResponse = await client.GetAsync("/api/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
    }

    [Fact]
    public async Task ProtectedHome_WithoutAuth_RedirectsToLogin()
    {
        // Arrange
        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var client = new HttpClient(handler) { BaseAddress = new Uri($"{fixture.WebBaseUrl}/") };

        // Act
        var response = await client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/login", response.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthenticatedSession_ThroughWebProxy_AllowsCurrentUserLookup()
    {
        // Arrange
        using var client = CreateWebClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = WellKnownUsers.DefaultUserEmail,
            Password = ApiWebApplicationFactory.DefaultTestPassword
        });
        loginResponse.EnsureSuccessStatusCode();

        // Act
        var meResponse = await client.GetAsync("/api/auth/me");
        var user = await meResponse.Content.ReadFromJsonAsync<AuthUserInfo>();

        // Assert
        meResponse.EnsureSuccessStatusCode();
        Assert.NotNull(user);
        Assert.Equal(WellKnownUsers.DefaultUserEmail, user!.Email);
    }

    [Fact]
    public async Task LoginPage_ReturnsHtmlWithAuthFormMarkers()
    {
        // Arrange
        using var client = new HttpClient { BaseAddress = new Uri($"{fixture.WebBaseUrl}/") };

        // Act
        var response = await client.GetAsync("/login");
        var html = await response.Content.ReadAsStringAsync();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Contains("data-testid=\"login-email\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("action=\"/account/login\"", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AccountLogin_WithSeededUser_AllowsAuthenticatedSession()
    {
        // Arrange
        using var handler = new HttpClientHandler
        {
            UseCookies = true,
            AllowAutoRedirect = false
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri($"{fixture.WebBaseUrl}/") };
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = WellKnownUsers.DefaultUserEmail,
            ["password"] = ApiWebApplicationFactory.DefaultTestPassword,
            ["returnUrl"] = "/"
        });

        // Act
        var loginResponse = await client.PostAsync("/account/login", form);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        Assert.Equal("/", loginResponse.Headers.Location?.OriginalString);
        Assert.True(loginResponse.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders));
        Assert.Contains(
            IdentityServiceExtensions.ApplicationCookieName,
            string.Join("; ", setCookieHeaders),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task AccountLogin_WithSeededUser_FollowedByHomeRequest_SendsAuthCookie()
    {
        // Arrange
        using var handler = new HttpClientHandler { UseCookies = true, AllowAutoRedirect = false };
        using var client = new HttpClient(handler) { BaseAddress = new Uri($"{fixture.WebBaseUrl}/") };
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = WellKnownUsers.DefaultUserEmail,
            ["password"] = ApiWebApplicationFactory.DefaultTestPassword,
            ["returnUrl"] = "/"
        });

        // Act
        var loginResponse = await client.PostAsync("/account/login", form);
        var homeResponse = await client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        Assert.NotEqual(HttpStatusCode.Redirect, homeResponse.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, homeResponse.StatusCode);
    }
}
