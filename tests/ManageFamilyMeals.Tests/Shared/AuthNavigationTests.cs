using ManageFamilyMeals.Shared.Auth;

namespace ManageFamilyMeals.Tests.Shared;

public class AuthNavigationTests
{
    [Theory]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("   ", "/")]
    [InlineData("/", "/")]
    [InlineData("/category/123", "/category/123")]
    public void GetSafeReturnUrl_WithSafePaths_ReturnsPath(string? returnUrl, string expected)
    {
        // Act
        var result = AuthNavigation.GetSafeReturnUrl(returnUrl);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("//evil.com")]
    [InlineData("https://evil.com")]
    [InlineData("/\\evil.com")]
    [InlineData("\\evil")]
    public void GetSafeReturnUrl_WithUnsafePaths_ReturnsFallback(string returnUrl)
    {
        // Act
        var result = AuthNavigation.GetSafeReturnUrl(returnUrl);

        // Assert
        Assert.Equal("/", result);
    }
}
