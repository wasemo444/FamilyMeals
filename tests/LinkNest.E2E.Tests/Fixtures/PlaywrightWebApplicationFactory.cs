using LinkNest.Api.Identity;
using LinkNest.Shared.Constants;
using LinkNest.Tests.Api;
using LinkNest.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LinkNest.E2E.Tests.Fixtures;

public sealed class PlaywrightWebApplicationFactory : WebApplicationFactory<WebApplicationMarker>, IDisposable
{
    private readonly string _sqlitePath;
    private readonly string _apiBaseAddress;
    private readonly string _dataProtectionPath;
    private IHost? _kestrelHost;

    public PlaywrightWebApplicationFactory(string sqlitePath, string apiBaseAddress, string dataProtectionPath)
    {
        _sqlitePath = sqlitePath;
        _apiBaseAddress = apiBaseAddress;
        _dataProtectionPath = dataProtectionPath;
    }

    public string ServerAddress { get; private set; } = string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(CreateConfiguration());
        });
    }

    private Dictionary<string, string?> CreateConfiguration() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Testing:SqlitePath"] = $"Data Source={_sqlitePath}",
        ["ReverseProxy:ApiBaseAddress"] = _apiBaseAddress.TrimEnd('/'),
        ["WebBaseUrl"] = "http://localhost/",
        ["DataProtection:KeysPath"] = _dataProtectionPath,
        [$"{IdentitySeedOptions.SectionName}:DefaultUserEmail"] = WellKnownUsers.DefaultUserEmail,
        [$"{IdentitySeedOptions.SectionName}:DefaultUserPassword"] = ApiWebApplicationFactory.DefaultTestPassword,
        [$"{IdentitySeedOptions.SectionName}:DefaultUserDisplayName"] = "Default Dev User",
        [$"{AuthOptions.SectionName}:AllowRegistration"] = "true",
        [$"{AuthOptions.SectionName}:RequireConfirmedEmail"] = "true",
        [$"{AuthOptions.SectionName}:WebBaseUrl"] = "http://localhost/",
        ["Database:SkipInitialization"] = "true"
    };

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var testHost = builder.Build();

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
