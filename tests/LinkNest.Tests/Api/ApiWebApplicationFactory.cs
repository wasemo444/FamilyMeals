using System.Net.Http.Json;
using LinkNest.Api;
using LinkNest.Api.Identity;
using LinkNest.Shared.Auth;
using LinkNest.Shared.Constants;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace LinkNest.Tests.Api;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<ApiApplicationMarker>, IDisposable
{
    public const string DefaultTestPassword = "DevPassword1!";

    private readonly string _sqlitePath = Path.Combine(
        Path.GetTempPath(),
        $"linknest-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        ApiTestHostConfiguration.Configure(builder, _sqlitePath, dataProtectionPath: null);

    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        string email = WellKnownUsers.DefaultUserEmail,
        string password = DefaultTestPassword)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        response.EnsureSuccessStatusCode();
        return client;
    }

    public async Task<HttpClient> CreateFreshAuthenticatedClientAsync(string? password = null)
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        await AuthTestHelpers.RegisterAndConfirmUserAsync(Services, email, password ?? DefaultTestPassword);
        return await CreateAuthenticatedClientAsync(email, password ?? DefaultTestPassword);
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
                // The test host may still hold the SQLite file handle briefly on shutdown.
            }
        }

        base.Dispose(disposing);
    }
}

public sealed class SharedApiWebApplicationFactory : WebApplicationFactory<ApiApplicationMarker>, IDisposable
{
    private readonly string _sqlitePath;
    private readonly string? _dataProtectionPath;

    public SharedApiWebApplicationFactory(string sqlitePath, string? dataProtectionPath = null)
    {
        _sqlitePath = sqlitePath;
        _dataProtectionPath = dataProtectionPath;
    }

    public string SqlitePath => _sqlitePath;

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        ApiTestHostConfiguration.Configure(builder, _sqlitePath, _dataProtectionPath);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}

public static class ApiTestHostConfiguration
{
    public static void Configure(IWebHostBuilder builder, string sqlitePath, string? dataProtectionPath)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Testing:SqlitePath"] = $"Data Source={sqlitePath}",
                [$"{IdentitySeedOptions.SectionName}:DefaultUserEmail"] = WellKnownUsers.DefaultUserEmail,
                [$"{IdentitySeedOptions.SectionName}:DefaultUserPassword"] = ApiWebApplicationFactory.DefaultTestPassword,
                [$"{IdentitySeedOptions.SectionName}:DefaultUserDisplayName"] = "Default Dev User",
                ["DataProtection:KeysPath"] = dataProtectionPath,
                [$"{JwtOptions.SectionName}:Secret"] = "LinkNest.Test.Jwt.SigningKey.Minimum32Chars!",
                [$"{JwtOptions.SectionName}:Issuer"] = "LinkNest.Api",
                [$"{JwtOptions.SectionName}:Audience"] = "LinkNest.Mobile"
            });
        });
    }
}
