using ManageFamilyMeals.Api.Identity;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace ManageFamilyMeals.Tests.Api;

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
        Assert.Contains("ManageFamilyMeals", path);
        Assert.Contains("DataProtection-Keys", path);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "Test";

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
