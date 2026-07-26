using System.Net;
using Yarp.ReverseProxy.Forwarder;

namespace LinkNest.Web.ReverseProxy;

/// <summary>
/// Registers and maps a YARP forwarder that proxies <c>/api/**</c> requests to the meal-data API.
/// </summary>
/// <remarks>
/// The Web host exposes a same-origin <c>/api</c> path so Blazor WebAssembly clients avoid CORS.
/// Antiforgery is disabled on the proxy route because API calls use cookie authentication.
/// </remarks>
public static class ApiProxyExtensions
{
    private static readonly ForwarderRequestConfig ForwarderConfig = new()
    {
        ActivityTimeout = TimeSpan.FromSeconds(100)
    };

    /// <summary>
    /// Adds the HTTP forwarder and a dedicated <see cref="HttpMessageInvoker"/> for API proxying.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddLinkNestApiProxy(this IServiceCollection services)
    {
        services.AddHttpForwarder();
        services.AddSingleton(_ => new HttpMessageInvoker(new SocketsHttpHandler
        {
            UseProxy = false,
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All,
            UseCookies = false
        }));

        return services;
    }

    /// <summary>
    /// Maps the catch-all forwarder at <c>/api/{**catch-all}</c>.
    /// </summary>
    /// <param name="app">The web application whose pipeline receives the proxy route.</param>
    /// <returns>The endpoint convention builder for the proxy route.</returns>
    /// <remarks>
    /// Destination base address is read from configuration key <c>ReverseProxy:ApiBaseAddress</c>
    /// (default <c>http://localhost:5280</c>).
    /// </remarks>
    public static IEndpointConventionBuilder MapLinkNestApiProxy(this WebApplication app)
    {
        var apiBaseAddress = app.Configuration["ReverseProxy:ApiBaseAddress"]
            ?? "http://localhost:5280";

        return app.Map("/api/{**catch-all}", async (
            HttpContext httpContext,
            IHttpForwarder forwarder,
            HttpMessageInvoker httpClient) =>
        {
            var destinationPrefix = apiBaseAddress.TrimEnd('/') + "/";
            var error = await forwarder.SendAsync(
                httpContext,
                destinationPrefix,
                httpClient,
                ForwarderConfig,
                HttpTransformer.Default);

            if (error != ForwarderError.None)
            {
                var statusCode = error switch
                {
                    ForwarderError.RequestCanceled or ForwarderError.RequestBodyCanceled
                        or ForwarderError.ResponseBodyCanceled or ForwarderError.UpgradeRequestCanceled
                        or ForwarderError.UpgradeResponseCanceled or ForwarderError.UpgradeActivityTimeout
                        => StatusCodes.Status499ClientClosedRequest,
                    ForwarderError.NoAvailableDestinations => StatusCodes.Status503ServiceUnavailable,
                    _ => StatusCodes.Status502BadGateway
                };

                if (!httpContext.Response.HasStarted)
                {
                    httpContext.Response.StatusCode = statusCode;
                }
            }
        }).DisableAntiforgery();
    }
}
