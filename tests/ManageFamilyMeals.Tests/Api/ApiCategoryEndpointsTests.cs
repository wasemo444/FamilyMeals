using System.Net;
using System.Net.Http.Json;
using ManageFamilyMeals.Shared.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ManageFamilyMeals.Tests.Api;

public class ApiCategoryEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiCategoryEndpointsTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostCategory_WithoutBootstrap_PreservesExistingCategories()
    {
        // Arrange
        var firstCategory = $"Breakfast-{Guid.NewGuid():N}";
        var secondCategory = $"Lunch-{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/categories", new { name = firstCategory });

        // Act
        var response = await _client.PostAsJsonAsync("/api/categories", new { name = secondCategory });
        var categories = await _client.GetFromJsonAsync<List<MealCategory>>("/api/categories");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(categories);
        Assert.Equal(2, categories!.Count);
        Assert.Contains(categories, category => category.Name == firstCategory);
        Assert.Contains(categories, category => category.Name == secondCategory);
    }

    [Fact]
    public async Task ArchiveCategory_ReturnsNotFoundWhenMissing()
    {
        // Act
        var response = await _client.PostAsync($"/api/categories/{Guid.NewGuid()}/archive", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RestoreCategory_ReturnsNotFoundWhenMissing()
    {
        // Act
        var response = await _client.PostAsync($"/api/categories/{Guid.NewGuid()}/restore", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddLink_ReturnsNotFoundWhenCategoryMissing()
    {
        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/categories/{Guid.NewGuid()}/links",
            new { titleEn = "Soup", titleAr = "حساء", url = "https://example.com/soup" });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ArchiveLink_ReturnsNotFoundWhenMissing()
    {
        // Act
        var response = await _client.PostAsync($"/api/links/{Guid.NewGuid()}/archive", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
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
                ["Testing:SqlitePath"] = $"Data Source={_sqlitePath}"
            });
        });
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
