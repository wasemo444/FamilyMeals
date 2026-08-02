using LinkNest.Api.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace LinkNest.Tests.Api;

public class IdentityServiceExtensionsTests
{
    [Fact]
    public void ResolveDataProtectionPath_WithRelativePath_UsesContentRoot()
    {
        // Arrange
        var environment = new FakeHostEnvironment { ContentRootPath = @"C:\app\web" };

        // Act
        var path = IdentityServiceExtensions.ResolveDataProtectionPath(".keys", environment);

        // Assert
        Assert.Equal(Path.Combine(@"C:\app\web", ".keys"), path);
    }

    [Fact]
    public void ResolveDataProtectionPath_WithEmptyPath_UsesLocalApplicationData()
    {
        // Arrange
        var environment = new FakeHostEnvironment { ContentRootPath = @"C:\app\web" };

        // Act
        var path = IdentityServiceExtensions.ResolveDataProtectionPath(null, environment);

        // Assert
        Assert.Contains("LinkNest", path);
        Assert.Contains("DataProtection-Keys", path);
    }

    [Fact]
    public void ResolveDataProtectionPath_WithEnvironmentVariable_ExpandsToAbsolutePath()
    {
        // Arrange
        var environment = new FakeHostEnvironment { ContentRootPath = @"C:\app\web" };
        var expectedRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Act
        var path = IdentityServiceExtensions.ResolveDataProtectionPath(
            "%LOCALAPPDATA%/LinkNest/DataProtection-Keys",
            environment);

        // Assert
        Assert.Equal(Path.Combine(expectedRoot, "LinkNest", "DataProtection-Keys"), path);
    }

    [Fact]
    public void EnsureDataProtectionKeysConfigured_InProductionWithDatabaseStorage_SkipsPathValidation()
    {
        // Arrange
        var environment = new FakeHostEnvironment { EnvironmentName = Environments.Production };

        // Act
        var act = () => IdentityServiceExtensions.EnsureDataProtectionKeysConfigured(
            null,
            environment,
            useDatabase: true);

        // Assert
        act();
    }

    [Theory]
    [InlineData("Database", true)]
    [InlineData("database", true)]
    [InlineData("FileSystem", false)]
    [InlineData(null, false)]
    public void UsesDatabaseStorage_ReturnsExpectedResult(string? storageMode, bool expected)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataProtection:Storage"] = storageMode
            })
            .Build();

        // Act
        var result = IdentityServiceExtensions.UsesDatabaseStorage(configuration);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void EnsureDataProtectionKeysConfigured_InProductionWithoutPath_Throws()
    {
        // Arrange
        var environment = new FakeHostEnvironment { EnvironmentName = Environments.Production };

        // Act
        var act = () => IdentityServiceExtensions.EnsureDataProtectionKeysConfigured(null, environment);

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("DataProtection:KeysPath", exception.Message);
    }

    [Fact]
    public void EnsureDataProtectionKeysConfigured_InProductionWithUnresolvedVariable_Throws()
    {
        // Arrange
        var environment = new FakeHostEnvironment { EnvironmentName = Environments.Production };

        // Act
        var act = () => IdentityServiceExtensions.EnsureDataProtectionKeysConfigured(
            "%MFM_DATA_PROTECTION_KEYS_PATH%",
            environment);

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("unresolved", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "Test";

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
