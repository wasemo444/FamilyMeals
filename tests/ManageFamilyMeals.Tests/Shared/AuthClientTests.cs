using System.Net;
using System.Text;
using System.Text.Json;
using ManageFamilyMeals.Shared.Auth;
using ManageFamilyMeals.Shared.Services;
using ManageFamilyMeals.Tests.Helpers;

namespace ManageFamilyMeals.Tests.Shared;

public class AuthClientTests
{
    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsUserInfo()
    {
        // Arrange
        var expected = new AuthUserInfo
        {
            Id = Guid.NewGuid(),
            Email = "dev@mfm.local",
            DisplayName = "Default Dev User"
        };
        var handler = new FakeHttpMessageHandler()
            .MapPost("/api/auth/login", _ => expected);
        var client = new AuthClient(new FakeHttpClientFactory(handler));

        // Act
        var user = await client.LoginAsync(new LoginRequest
        {
            Email = expected.Email,
            Password = "DevPassword1!"
        });

        // Assert
        Assert.Equal(expected.Email, user.Email);
        Assert.Equal(expected.DisplayName, user.DisplayName);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler()
            .MapPost("/api/auth/login", _ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = new AuthClient(new FakeHttpClientFactory(handler));

        // Act
        var act = () => client.LoginAsync(new LoginRequest
        {
            Email = "dev@mfm.local",
            Password = "WrongPassword1!"
        });

        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WhenUnauthenticated_ReturnsNull()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler()
            .MapGet("/api/auth/me", new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = new AuthClient(new FakeHttpClientFactory(handler));

        // Act
        var user = await client.GetCurrentUserAsync();

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public async Task RegisterAsync_WithSimpleBadRequest_ThrowsAuthValidationException()
    {
        // Arrange
        var payload = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["error"] = "Email and password are required."
        });
        var handler = new FakeHttpMessageHandler()
            .MapPost("/api/auth/register", _ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });
        var client = new AuthClient(new FakeHttpClientFactory(handler));

        // Act
        var act = () => client.RegisterAsync(new RegisterRequest());

        // Assert
        var exception = await Assert.ThrowsAsync<AuthValidationException>(act);
        Assert.Contains("required", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(exception.Errors.ContainsKey("error"));
    }

    [Fact]
    public async Task RegisterAsync_WithValidationErrors_ThrowsAuthValidationException()
    {
        // Arrange
        var payload = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["errors"] = new Dictionary<string, string[]>
            {
                ["PasswordTooShort"] = ["Passwords must be at least 8 characters."]
            }
        });
        var handler = new FakeHttpMessageHandler()
            .MapPost("/api/auth/register", _ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });
        var client = new AuthClient(new FakeHttpClientFactory(handler));

        // Act
        var act = () => client.RegisterAsync(new RegisterRequest
        {
            Email = "user@example.com",
            Password = "short"
        });

        // Assert
        var exception = await Assert.ThrowsAsync<AuthValidationException>(act);
        Assert.Contains("8 characters", exception.Message);
        Assert.True(exception.Errors.ContainsKey("PasswordTooShort"));
    }

    [Fact]
    public async Task LoginAsync_WhenAccountLockedOut_ThrowsAuthValidationException()
    {
        // Arrange
        var payload = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["detail"] = "Account is temporarily locked. Try again later."
        });
        var handler = new FakeHttpMessageHandler()
            .MapPost("/api/auth/login", _ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });
        var client = new AuthClient(new FakeHttpClientFactory(handler));

        // Act
        var act = () => client.LoginAsync(new LoginRequest
        {
            Email = "dev@mfm.local",
            Password = "DevPassword1!"
        });

        // Assert
        var exception = await Assert.ThrowsAsync<AuthValidationException>(act);
        Assert.Contains("locked", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
