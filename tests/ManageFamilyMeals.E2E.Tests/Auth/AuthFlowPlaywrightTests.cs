using System.Net;
using System.Net.Http.Json;
using ManageFamilyMeals.E2E.Tests.Fixtures;
using ManageFamilyMeals.Shared.Auth;
using ManageFamilyMeals.Shared.Constants;
using ManageFamilyMeals.Tests.Api;
using Microsoft.Playwright;

namespace ManageFamilyMeals.E2E.Tests.Auth;

[Collection("E2E")]
public class AuthFlowPlaywrightTests(FullStackApplicationFixture fixture)
{
    private static PageGotoOptions GotoOptions => new()
    {
        WaitUntil = WaitUntilState.DOMContentLoaded,
        Timeout = 60_000
    };

    [Fact]
    public async Task Register_ThenLogin_ShowsAuthenticatedShell()
    {
        // Arrange
        var email = $"e2e-{Guid.NewGuid():N}@example.com";
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        // Act
        var registerResponse = await context.APIRequest.PostAsync(
            $"{fixture.WebBaseUrl}/api/auth/register",
            new APIRequestContextOptions
            {
                DataObject = new RegisterRequest
                {
                    Email = email,
                    Password = "RegisterPass1!",
                    ConfirmPassword = "RegisterPass1!",
                    DisplayName = "E2E User"
                }
            });
        Assert.True(registerResponse.Ok);

        await AuthTestHelpers.ConfirmEmailAsync(fixture.WebFactory.Services, email);

        await page.GotoAsync($"{fixture.WebBaseUrl}/login?registered=true&confirmEmail=true&email={Uri.EscapeDataString(email)}", GotoOptions);
        await Assertions.Expect(page.Locator("[data-testid='login-success']")).ToBeVisibleAsync();

        var loginResponse = await context.APIRequest.PostAsync(
            $"{fixture.WebBaseUrl}/api/auth/login",
            new APIRequestContextOptions
            {
                DataObject = new LoginRequest
                {
                    Email = email,
                    Password = "RegisterPass1!"
                }
            });
        Assert.True(loginResponse.Ok);
        var loginBody = await loginResponse.TextAsync();

        // Assert
        Assert.Contains(email, loginBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_ThenLogout_RedirectsToLogin()
    {
        // Arrange
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var loginResponse = await context.APIRequest.PostAsync(
            $"{fixture.WebBaseUrl}/api/auth/login",
            new APIRequestContextOptions
            {
                DataObject = new LoginRequest
                {
                    Email = WellKnownUsers.DefaultUserEmail,
                    Password = ApiWebApplicationFactory.DefaultTestPassword
                }
            });
        Assert.True(loginResponse.Ok);

        // Act
        var logoutResponse = await context.APIRequest.PostAsync($"{fixture.WebBaseUrl}/api/auth/logout");
        var meResponse = await context.APIRequest.GetAsync($"{fixture.WebBaseUrl}/api/auth/me");
        await page.GotoAsync($"{fixture.WebBaseUrl}/login", GotoOptions);

        // Assert
        Assert.True(logoutResponse.Ok);
        Assert.Equal((int)HttpStatusCode.Unauthorized, meResponse.Status);
        await Assertions.Expect(page.Locator("[data-testid='login-email']")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ProtectedHome_WithoutAuth_RedirectsToLoginInBrowser()
    {
        // Arrange
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();

        // Act
        await page.GotoAsync($"{fixture.WebBaseUrl}/", GotoOptions);

        // Assert
        await page.WaitForURLAsync(
            url => url.Contains("/login", StringComparison.Ordinal),
            new PageWaitForURLOptions { Timeout = 60_000 });
        await Assertions.Expect(page.Locator("[data-testid='login-email']")).ToBeVisibleAsync();
    }
}
