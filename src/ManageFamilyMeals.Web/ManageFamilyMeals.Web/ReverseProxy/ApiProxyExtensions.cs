using System.Net;
using Yarp.ReverseProxy.Forwarder;

namespace ManageFamilyMeals.Web.ReverseProxy;

public static class ApiProxyExtensions
{
    private static readonly ForwarderRequestConfig ForwarderConfig = new()
    {
        ActivityTimeout = TimeSpan.FromSeconds(100)
    };

    public static IServiceCollection AddMealDataApiProxy(this IServiceCollection services)
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

    public static IEndpointConventionBuilder MapMealDataApiProxy(this WebApplication app)
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
