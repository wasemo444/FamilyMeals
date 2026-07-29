using System.Net;
using System.Net.Http.Json;
using LinkNest.Api.Identity;
using LinkNest.Shared.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace LinkNest.Tests.Api;

public class AuthPasswordResetTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public AuthPasswordResetTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ForgotPassword_ForExistingUser_ReturnsOk()
    {
        var email = $"reset-{Guid.NewGuid():N}@example.com";
        await AuthTestHelpers.RegisterAndConfirmUserAsync(_factory.Services, email);

        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest
        {
            Email = email
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_AllowsLoginWithNewPassword()
    {
        var email = $"reset-{Guid.NewGuid():N}@example.com";
        const string oldPassword = "OldPassword1!";
        const string newPassword = "NewPassword1!";
        await AuthTestHelpers.RegisterAndConfirmUserAsync(_factory.Services, email, oldPassword);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);

        var token = await userManager.GeneratePasswordResetTokenAsync(user!);

        using var client = _factory.CreateClient();
        var resetResponse = await client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest
        {
            UserId = user!.Id,
            Code = token,
            Password = newPassword,
            ConfirmPassword = newPassword
        });
        resetResponse.EnsureSuccessStatusCode();

        var oldLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = oldPassword
        });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var newLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = newPassword
        });
        newLogin.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ResendConfirmation_ForUnconfirmedUser_ReturnsOk()
    {
        var email = $"unconfirmed-{Guid.NewGuid():N}@example.com";
        await AuthTestHelpers.RegisterUserAsync(_factory.Services, email, confirmEmail: false);

        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/resend-confirmation", new ResendConfirmationRequest
        {
            Email = email
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_AfterLogin_UpdatesDisplayName()
    {
        using var client = await _factory.CreateFreshAuthenticatedClientAsync();
        var response = await client.PatchAsJsonAsync("/api/auth/me", new UpdateProfileRequest
        {
            DisplayName = "Updated Name"
        });
        var user = await response.Content.ReadFromJsonAsync<AuthUserInfo>();

        response.EnsureSuccessStatusCode();
        Assert.NotNull(user);
        Assert.Equal("Updated Name", user!.DisplayName);
    }

    [Fact]
    public async Task DeactivateAccount_BlocksSubsequentLogin()
    {
        var email = $"deactivate-{Guid.NewGuid():N}@example.com";
        const string password = "DeactivatePass1!";
        await AuthTestHelpers.RegisterAndConfirmUserAsync(_factory.Services, email, password);

        using var client = await _factory.CreateAuthenticatedClientAsync(email, password);
        var deactivateResponse = await client.PostAsJsonAsync("/api/auth/deactivate", new DeactivateAccountRequest
        {
            Password = password,
            Confirmation = "DEACTIVATE"
        });
        deactivateResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }
}
