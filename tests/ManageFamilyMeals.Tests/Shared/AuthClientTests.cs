using System.Net;
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
}
