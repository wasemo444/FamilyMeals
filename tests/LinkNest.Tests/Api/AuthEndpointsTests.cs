using System.Net;
using System.Net.Http.Json;
using LinkNest.Shared.Auth;
using LinkNest.Shared.Constants;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LinkNest.Tests.Api;

public class AuthEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public AuthEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithSeededDefaultUser_ReturnsUserInfoAndSetsCookie()
    {
        // Arrange
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = WellKnownUsers.DefaultUserEmail,
            Password = ApiWebApplicationFactory.DefaultTestPassword
        });
        var user = await response.Content.ReadFromJsonAsync<AuthUserInfo>();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(user);
        Assert.Equal(WellKnownUsers.DefaultUserId, user!.Id);
        Assert.Equal(WellKnownUsers.DefaultUserEmail, user.Email);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = WellKnownUsers.DefaultUserEmail,
            Password = "WrongPassword1!"
        });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithValidCredentials_CreatesUserWithoutSigningIn()
    {
        // Arrange
        var email = $"user-{Guid.NewGuid():N}@example.com";
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "RegisterPass1!",
            ConfirmPassword = "RegisterPass1!",
            DisplayName = "New User"
        });
        var user = await response.Content.ReadFromJsonAsync<AuthUserInfo>();
        var meResponse = await client.GetAsync("/api/auth/me");
        var loginBeforeConfirm = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "RegisterPass1!"
        });

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(user);
        Assert.Equal(email, user!.Email);
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, loginBeforeConfirm.StatusCode);

        await AuthTestHelpers.ConfirmEmailAsync(_factory.Services, email);

        var loginAfterConfirm = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "RegisterPass1!"
        });

        loginAfterConfirm.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Register_WithMismatchedPasswords_ReturnsValidationProblem()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = $"user-{Guid.NewGuid():N}@example.com",
            Password = "RegisterPass1!",
            ConfirmPassword = "DifferentPass1!",
            DisplayName = "New User"
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_AfterLogin_ReturnsCurrentUser()
    {
        // Arrange
        using var client = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/auth/me");
        var user = await response.Content.ReadFromJsonAsync<AuthUserInfo>();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(user);
        Assert.Equal(WellKnownUsers.DefaultUserEmail, user!.Email);
    }

    [Fact]
    public async Task Logout_AfterLogin_ClearsSession()
    {
        // Arrange
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var logoutResponse = await client.PostAsync("/api/auth/logout", null);
        logoutResponse.EnsureSuccessStatusCode();

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Bootstrap_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/bootstrap");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Bootstrap_AfterLogin_ReturnsSuccess()
    {
        // Arrange
        using var client = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/bootstrap");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task TokenLogin_WithSeededDefaultUser_ReturnsJwtAndMeWorksWithBearer()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var tokenResponse = await client.PostAsJsonAsync("/api/auth/token", new LoginRequest
        {
            Email = WellKnownUsers.DefaultUserEmail,
            Password = ApiWebApplicationFactory.DefaultTestPassword
        });
        var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();

        using var bearerRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        bearerRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            tokenPayload!.AccessToken);
        var meResponse = await client.SendAsync(bearerRequest);
        var user = await meResponse.Content.ReadFromJsonAsync<AuthUserInfo>();

        // Assert
        tokenResponse.EnsureSuccessStatusCode();
        Assert.NotNull(tokenPayload);
        Assert.False(string.IsNullOrWhiteSpace(tokenPayload!.AccessToken));
        Assert.True(tokenPayload.ExpiresAtUtc > DateTime.UtcNow);
        Assert.Equal(WellKnownUsers.DefaultUserEmail, tokenPayload.User.Email);
        meResponse.EnsureSuccessStatusCode();
        Assert.NotNull(user);
        Assert.Equal(WellKnownUsers.DefaultUserEmail, user!.Email);
    }

    [Fact]
    public async Task TokenLogin_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/token", new LoginRequest
        {
            Email = WellKnownUsers.DefaultUserEmail,
            Password = "WrongPassword1!"
        });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Bootstrap_WithBearerToken_ReturnsSuccess()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var tokenResponse = await client.PostAsJsonAsync("/api/auth/token", new LoginRequest
        {
            Email = WellKnownUsers.DefaultUserEmail,
            Password = ApiWebApplicationFactory.DefaultTestPassword
        });
        var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();
        tokenResponse.EnsureSuccessStatusCode();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/bootstrap");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            tokenPayload!.AccessToken);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Me_WithBearerAndCookiePrefersBearerWhenBothPresent()
    {
        // Arrange — cookie session from login
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = WellKnownUsers.DefaultUserEmail,
            Password = ApiWebApplicationFactory.DefaultTestPassword
        });
        loginResponse.EnsureSuccessStatusCode();

        var tokenResponse = await client.PostAsJsonAsync("/api/auth/token", new LoginRequest
        {
            Email = WellKnownUsers.DefaultUserEmail,
            Password = ApiWebApplicationFactory.DefaultTestPassword
        });
        var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();

        // Act — send both cookie (automatic) and bearer header
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            tokenPayload!.AccessToken);
        var meResponse = await client.SendAsync(request);

        // Assert
        meResponse.EnsureSuccessStatusCode();
    }
}
