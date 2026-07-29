using LinkNest.Api.Services;

namespace LinkNest.Tests.Api;

public class SafeUrlValidatorTests
{
    private readonly SafeUrlValidator _validator = new();

    [Theory]
    [InlineData("http://127.0.0.1/page")]
    [InlineData("http://localhost/page")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://10.0.0.1/internal")]
    [InlineData("http://192.168.1.1/router")]
    [InlineData("http://100.64.0.1/internal")]
    [InlineData("http://[::10.0.0.1]/internal")]
    [InlineData("http://[::127.0.0.1]/internal")]
    public async Task IsAllowedUrlAsync_BlocksPrivateAndMetadataHosts(string url)
    {
        var uri = new Uri(url);

        var allowed = await _validator.IsAllowedUrlAsync(uri);

        Assert.False(allowed);
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com/resource")]
    [InlineData("javascript:alert(1)")]
    public async Task IsAllowedUrlAsync_RejectsNonHttpSchemes(string url)
    {
        var uri = new Uri(url);

        var allowed = await _validator.IsAllowedUrlAsync(uri);

        Assert.False(allowed);
    }

    [Fact]
    public async Task IsAllowedUrlAsync_AllowsPublicIpLiteral()
    {
        var uri = new Uri("http://8.8.8.8/");

        var allowed = await _validator.IsAllowedUrlAsync(uri);

        Assert.True(allowed);
    }
}
