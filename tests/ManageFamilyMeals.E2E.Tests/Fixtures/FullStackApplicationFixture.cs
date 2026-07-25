using ManageFamilyMeals.E2E.Tests.Fixtures;
using ManageFamilyMeals.Tests.Api;

namespace ManageFamilyMeals.E2E.Tests;

[CollectionDefinition("E2E", DisableParallelization = true)]
public sealed class E2ECollection : ICollectionFixture<FullStackApplicationFixture>;

public sealed class FullStackApplicationFixture : IAsyncLifetime, IDisposable
{
    private readonly string _sqlitePath = Path.Combine(
        Path.GetTempPath(),
        $"managefamilymeals-e2e-{Guid.NewGuid():N}.db");

    private readonly string _dataProtectionPath = Path.Combine(
        Path.GetTempPath(),
        $"managefamilymeals-e2e-keys-{Guid.NewGuid():N}");

    public PlaywrightApiApplicationFactory ApiFactory { get; private set; } = null!;
    public PlaywrightWebApplicationFactory WebFactory { get; private set; } = null!;
    public string WebBaseUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException("Failed to install Playwright Chromium browser.");
        }

        Directory.CreateDirectory(_dataProtectionPath);

        ApiFactory = new PlaywrightApiApplicationFactory(_sqlitePath, _dataProtectionPath);
        _ = ApiFactory.Server;

        WebFactory = new PlaywrightWebApplicationFactory(
            _sqlitePath,
            ApiFactory.ServerAddress,
            _dataProtectionPath);
        _ = WebFactory.Server;
        WebBaseUrl = WebFactory.ServerAddress;

        await Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose()
    {
        WebFactory?.Dispose();
        ApiFactory?.Dispose();

        try
        {
            if (File.Exists(_sqlitePath))
            {
                File.Delete(_sqlitePath);
            }

            if (Directory.Exists(_dataProtectionPath))
            {
                Directory.Delete(_dataProtectionPath, recursive: true);
            }
        }
        catch (IOException)
        {
            // Test hosts may still hold file handles briefly on shutdown.
        }
    }
}
