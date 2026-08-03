using System.Net;
using System.Net.Http.Json;
using LinkNest.Api;
using LinkNest.Api.Identity;
using LinkNest.Shared.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinkNest.Tests.Api;

public class ApiHardeningTests : IClassFixture<ApiWebApplicationFactory>, IClassFixture<ProductionCorsWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly ProductionCorsWebApplicationFactory _corsFactory;

    public ApiHardeningTests(
        ApiWebApplicationFactory factory,
        ProductionCorsWebApplicationFactory corsFactory)
    {
        _factory = factory;
        _corsFactory = corsFactory;
    }

    [Fact]
    public async Task Health_Get_ReturnsOkWithStatus()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("ok", payload!.Status);
    }

    [Fact]
    public async Task HealthReady_Get_ReturnsOkWhenDatabaseAvailable()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/ready");
        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("ready", payload!.Status);
    }

    [Fact]
    public async Task Cors_Preflight_FromConfiguredOrigin_AllowsOriginWithoutCredentials()
    {
        using var client = _corsFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/token");
        request.Headers.Add("Origin", ProductionCorsWebApplicationFactory.TestOrigin);
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var allowedOrigins));
        Assert.Equal(ProductionCorsWebApplicationFactory.TestOrigin, allowedOrigins!.Single());
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task ConfirmEmail_WithValidToken_ConfirmsUserAndAllowsLogin()
    {
        var email = $"confirm-{Guid.NewGuid():N}@example.com";
        await AuthTestHelpers.RegisterUserAsync(_factory.Services, email, confirmEmail: false);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user!);

        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var confirmResponse = await client.PostAsJsonAsync("/api/auth/confirm-email", new ConfirmEmailRequest
        {
            UserId = user!.Id,
            Code = token
        });
        confirmResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = ApiWebApplicationFactory.DefaultTestPassword
        });
        loginResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ConfirmEmail_WithInvalidUserId_ReturnsValidationProblem()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/confirm-email", new ConfirmEmailRequest
        {
            UserId = Guid.NewGuid(),
            Code = "invalid-token"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TokenLogin_WithUnknownEmail_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/token", new LoginRequest
        {
            Email = $"unknown-{Guid.NewGuid():N}@example.com",
            Password = "SomePassword1!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void EmailConfirmationService_BuildConfirmationLink_UsesConfirmEmailPath()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<EmailConfirmationService>();
        var link = service.BuildConfirmationLink(Guid.Parse("11111111-1111-1111-1111-111111111111"), "test-token");

        Assert.Contains("/confirm-email?", link, StringComparison.Ordinal);
        Assert.DoesNotContain("/account/confirm-email", link, StringComparison.Ordinal);
    }

    [Fact]
    public void PasswordResetService_BuildResetLink_UsesResetPasswordPath()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<PasswordResetService>();
        var link = service.BuildResetLink(Guid.Parse("11111111-1111-1111-1111-111111111111"), "test-token");

        Assert.Contains("/reset-password?", link, StringComparison.Ordinal);
        Assert.Contains("userId=", link, StringComparison.Ordinal);
        Assert.Contains("code=", link, StringComparison.Ordinal);
    }

    private sealed class HealthResponse
    {
        public string Status { get; set; } = string.Empty;
    }
}

public sealed class ProductionCorsWebApplicationFactory : WebApplicationFactory<ApiApplicationMarker>, IDisposable
{
    public const string TestOrigin = "https://linknest-test.pages.dev";

    private readonly string _sqlitePath = Path.Combine(
        Path.GetTempPath(),
        $"linknest-cors-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("ProductionTesting");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Testing:SqlitePath"] = $"Data Source={_sqlitePath}",
                ["Database:SkipInitialization"] = "false",
                ["DataProtection:Storage"] = DataProtectionStorageMode.Database,
                [$"{CorsOptions.SectionName}:AllowedOrigins"] = TestOrigin,
                [$"{EmailOptions.SectionName}:UseSmtp"] = "false",
                [$"{JwtOptions.SectionName}:Secret"] = "LinkNest.Test.Jwt.SigningKey.Minimum32Chars!",
                [$"{JwtOptions.SectionName}:Issuer"] = "LinkNest.Api",
                [$"{JwtOptions.SectionName}:Audience"] = "LinkNest.Mobile",
                [$"{IdentitySeedOptions.SectionName}:DefaultUserEmail"] = "cors-test@example.com",
                [$"{IdentitySeedOptions.SectionName}:DefaultUserPassword"] = ApiWebApplicationFactory.DefaultTestPassword,
                [$"{IdentitySeedOptions.SectionName}:DefaultUserDisplayName"] = "CORS Test User"
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                if (File.Exists(_sqlitePath))
                {
                    File.Delete(_sqlitePath);
                }
            }
            catch (IOException)
            {
                // The test host may still hold handles briefly on shutdown.
            }
        }

        base.Dispose(disposing);
    }
}
