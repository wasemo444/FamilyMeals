using System.Net;
using System.Text;
using System.Text.Json;

namespace ManageFamilyMeals.Tests.Helpers;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _routes = new(StringComparer.OrdinalIgnoreCase);

    public FakeHttpMessageHandler MapGet(string path, object responseBody) =>
        Map(HttpMethod.Get, path, _ => JsonResponse(responseBody));

    public FakeHttpMessageHandler MapGet(string path, HttpResponseMessage response) =>
        Map(HttpMethod.Get, path, _ => response);

    public FakeHttpMessageHandler MapGetSequence(string path, params object[] responses)
    {
        var index = 0;
        return Map(HttpMethod.Get, path, _ =>
        {
            var body = responses[Math.Min(index, responses.Length - 1)];
            if (index < responses.Length - 1)
            {
                index++;
            }

            return JsonResponse(body);
        });
    }

    public FakeHttpMessageHandler MapPost(string path, Func<HttpRequestMessage, object> responseFactory) =>
        Map(HttpMethod.Post, path, request => JsonResponse(responseFactory(request)));

    public FakeHttpMessageHandler MapPost(string path, Func<HttpRequestMessage, HttpResponseMessage> responseFactory) =>
        Map(HttpMethod.Post, path, responseFactory);

    public FakeHttpMessageHandler MapPut(string path, HttpStatusCode statusCode = HttpStatusCode.NoContent) =>
        Map(HttpMethod.Put, path, _ => new HttpResponseMessage(statusCode));

    public FakeHttpMessageHandler MapPostNoContent(string path) =>
        Map(HttpMethod.Post, path, _ => new HttpResponseMessage(HttpStatusCode.NoContent));

    private FakeHttpMessageHandler Map(
        HttpMethod method,
        string path,
        Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _routes[$"{method}:{NormalizePath(path)}"] = handler;
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var key = $"{request.Method}:{NormalizePath(request.RequestUri!.AbsolutePath)}";
        if (!_routes.TryGetValue(key, out var handler))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        return Task.FromResult(handler(request));
    }

    private static string NormalizePath(string path) =>
        path.TrimEnd('/').Length == 0 ? "/" : path.TrimEnd('/');

    private static HttpResponseMessage JsonResponse(object body) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json")
        };
}

internal sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    private readonly HttpClient _client = new(handler)
    {
        BaseAddress = new Uri("http://localhost")
    };

    public HttpClient CreateClient(string name) => _client;
}
