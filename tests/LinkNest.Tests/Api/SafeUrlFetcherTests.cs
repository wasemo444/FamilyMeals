using System.Net;
using System.Text;
using LinkNest.Api.Services;
using LinkNest.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace LinkNest.Tests.Api;

public class SafeRedirectHttpMessageHandlerTests
{
    [Fact]
    public async Task SendAsync_BlocksRedirectToLoopbackTarget()
    {
        var innerHandler = new FakeHttpMessageHandler()
            .MapGet("/public", new HttpResponseMessage(HttpStatusCode.Redirect)
            {
                Headers = { Location = new Uri("http://127.0.0.1/secret") }
            });

        var handler = new SafeRedirectHttpMessageHandler(new SafeUrlValidator(), innerHandler);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };

        using var response = await client.GetAsync("/public");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_FollowsRedirectToAllowedTarget()
    {
        var innerHandler = new FakeHttpMessageHandler()
            .MapGet("/public", new HttpResponseMessage(HttpStatusCode.Redirect)
            {
                Headers = { Location = new Uri("https://example.com/final") }
            })
            .MapGet("/final", new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html><title>OK</title></html>", Encoding.UTF8, "text/html")
            });

        var handler = new SafeRedirectHttpMessageHandler(new SafeUrlValidator(), innerHandler);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };

        using var response = await client.GetAsync("/public");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

public class SafeUrlFetcherTests
{
    [Fact]
    public async Task GetAsync_ReturnsResponseForPublicUrl()
    {
        var innerHandler = new FakeHttpMessageHandler()
            .MapGet("/", new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html><title>Recipe</title></html>", Encoding.UTF8, "text/html")
            });

        var handler = new SafeRedirectHttpMessageHandler(new SafeUrlValidator(), innerHandler);
        var factory = new FakeHttpClientFactory(handler);
        var fetcher = new SafeUrlFetcher(factory, new SafeUrlValidator(), NullLogger<SafeUrlFetcher>.Instance);

        using var response = await fetcher.GetAsync(new Uri("https://example.com/"));

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response!.StatusCode);
    }

    [Fact]
    public async Task GetAsync_ReturnsNullForLoopbackUrl()
    {
        var innerHandler = new FakeHttpMessageHandler();
        var handler = new SafeRedirectHttpMessageHandler(new SafeUrlValidator(), innerHandler);
        var factory = new FakeHttpClientFactory(handler);
        var fetcher = new SafeUrlFetcher(factory, new SafeUrlValidator(), NullLogger<SafeUrlFetcher>.Instance);

        var response = await fetcher.GetAsync(new Uri("http://127.0.0.1/"));

        Assert.Null(response);
    }
}

public class LinkPreviewServiceTests
{
    [Fact]
    public async Task FetchAsync_ReturnsMetadataForPublicUrl()
    {
        var innerHandler = new FakeHttpMessageHandler()
            .MapGet("/recipe", new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "<html><head><meta property=\"og:title\" content=\"Test Recipe\" /></head></html>",
                    Encoding.UTF8,
                    "text/html")
            });

        var handler = new SafeRedirectHttpMessageHandler(new SafeUrlValidator(), innerHandler);
        var factory = new FakeHttpClientFactory(handler);
        var fetcher = new SafeUrlFetcher(factory, new SafeUrlValidator(), NullLogger<SafeUrlFetcher>.Instance);
        var service = new LinkPreviewService(fetcher, NullLogger<LinkPreviewService>.Instance);

        var preview = await service.FetchAsync("https://example.com/recipe");

        Assert.NotNull(preview);
        Assert.Equal("Test Recipe", preview!.Title);
    }

    [Fact]
    public async Task FetchAsync_ReturnsNullForPrivateIpLiteral()
    {
        var innerHandler = new FakeHttpMessageHandler();
        var handler = new SafeRedirectHttpMessageHandler(new SafeUrlValidator(), innerHandler);
        var factory = new FakeHttpClientFactory(handler);
        var fetcher = new SafeUrlFetcher(factory, new SafeUrlValidator(), NullLogger<SafeUrlFetcher>.Instance);
        var service = new LinkPreviewService(fetcher, NullLogger<LinkPreviewService>.Instance);

        var preview = await service.FetchAsync("http://192.168.0.10/page");

        Assert.Null(preview);
    }
}
