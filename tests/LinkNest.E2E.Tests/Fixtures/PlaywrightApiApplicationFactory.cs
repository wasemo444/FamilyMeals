using LinkNest.Api;
using LinkNest.Api.Data;
using LinkNest.Api.Identity;
using LinkNest.Tests.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LinkNest.E2E.Tests.Fixtures;

public sealed class PlaywrightApiApplicationFactory : WebApplicationFactory<ApiApplicationMarker>, IDisposable
{
    private readonly string _sqlitePath;
    private readonly string? _dataProtectionPath;
    private IHost? _kestrelHost;

    public PlaywrightApiApplicationFactory(string sqlitePath, string? dataProtectionPath = null)
    {
        _sqlitePath = sqlitePath;
        _dataProtectionPath = dataProtectionPath;
    }

    public string ServerAddress { get; private set; } = string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ApiTestHostConfiguration.Configure(builder, _sqlitePath, _dataProtectionPath);
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:SkipInitialization"] = "true"
            });
        });
    }

    private Dictionary<string, string?> CreateConfiguration() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Testing:SqlitePath"] = $"Data Source={_sqlitePath}",
        ["DataProtection:KeysPath"] = _dataProtectionPath,
        ["Database:SkipInitialization"] = "true"
    };

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var testHost = builder.Build();
        InitializeTestingDatabaseAsync(testHost.Services).GetAwaiter().GetResult();

        builder.ConfigureWebHost(webBuilder =>
        {
            webBuilder.UseSetting(WebHostDefaults.ServerUrlsKey, "http://127.0.0.1:0");
            webBuilder.UseKestrel();
            webBuilder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(CreateConfiguration());
            });
        });

        _kestrelHost = builder.Build();
        _kestrelHost.Start();

        var addresses = _kestrelHost.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses;
        ServerAddress = addresses.Single().TrimEnd('/');

        return testHost;
    }

    private static async Task InitializeTestingDatabaseAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (environment.IsEnvironment("Testing"))
        {
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
        }

        await scope.ServiceProvider.GetRequiredService<IdentityDataSeeder>().SeedAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _kestrelHost?.StopAsync().GetAwaiter().GetResult();
            _kestrelHost?.Dispose();
        }

        base.Dispose(disposing);
    }
}
