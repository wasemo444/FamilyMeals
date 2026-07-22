using System.Net.Http.Json;
using ManageFamilyMeals.Api.Identity;
using ManageFamilyMeals.Shared.Auth;
using ManageFamilyMeals.Shared.Constants;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ManageFamilyMeals.Tests.Api;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    public const string DefaultTestPassword = "DevPassword1!";

    private readonly string _sqlitePath = Path.Combine(
        Path.GetTempPath(),
        $"managefamilymeals-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Testing:SqlitePath"] = $"Data Source={_sqlitePath}",
                [$"{IdentitySeedOptions.SectionName}:DefaultUserEmail"] = WellKnownUsers.DefaultUserEmail,
                [$"{IdentitySeedOptions.SectionName}:DefaultUserPassword"] = DefaultTestPassword,
                [$"{IdentitySeedOptions.SectionName}:DefaultUserDisplayName"] = "Default Dev User"
            });
        });
    }

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
