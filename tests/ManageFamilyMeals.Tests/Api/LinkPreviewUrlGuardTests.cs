using ManageFamilyMeals.Api.Services;

namespace ManageFamilyMeals.Tests.Api;

public class LinkPreviewUrlGuardTests
{
    [Theory]
    [InlineData("http://127.0.0.1/page")]
    [InlineData("http://localhost/page")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://10.0.0.1/internal")]
    [InlineData("http://192.168.1.1/router")]
    public async Task IsAllowedPublicUrlAsync_BlocksPrivateAndMetadataHosts(string url)
    {
        // Arrange
        var uri = new Uri(url);

        // Act
        var allowed = await LinkPreviewUrlGuard.IsAllowedPublicUrlAsync(uri);

        // Assert
        Assert.False(allowed);
    }

    [Fact]
    public async Task IsAllowedPublicUrlAsync_AllowsPublicHttpsUrls()
    {
        // Arrange
        var uri = new Uri("https://example.com/recipe");

        // Act
        var allowed = await LinkPreviewUrlGuard.IsAllowedPublicUrlAsync(uri);

        // Assert
        Assert.True(allowed);
    }
}
