using Yarp.ReverseProxy.Configuration;

namespace ManageFamilyMeals.Web.ReverseProxy;

public static class ApiProxyExtensions
{
    public const string ApiClusterId = "meal-data-api";

    public static IServiceCollection AddMealDataApiProxy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var apiBaseAddress = configuration["ReverseProxy:ApiBaseAddress"]
            ?? "http://localhost:5280";

        services.AddReverseProxy()
            .LoadFromMemory(
            [
                new RouteConfig
                {
                    RouteId = "meal-data-api",
                    ClusterId = ApiClusterId,
                    Match = new RouteMatch
                    {
                        Path = "/api/{**catch-all}"
                    }
                }
            ],
            [
                new ClusterConfig
                {
                    ClusterId = ApiClusterId,
                    Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["primary"] = new() { Address = apiBaseAddress.TrimEnd('/') + "/" }
                    }
                }
            ]);

        return services;
    }
}
