using LinkNest.Api.Data;
using LinkNest.Api.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LinkNest.Api.Endpoints;

/// <summary>
/// Health probe endpoints for load balancers and platform health checks.
/// </summary>
public static class HealthEndpoints
{
    /// <summary>
    /// Maps <c>/health</c> and optional <c>/health/ready</c> routes.
    /// </summary>
    /// <param name="endpoints">The application endpoint builder.</param>
    /// <returns>The same <paramref name="endpoints"/> instance for chaining.</returns>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        endpoints.MapGet("/health/ready", ReadyAsync);
        endpoints.MapGet("/health/db", DbInfoAsync);
        endpoints.MapGet("/health/email", EmailAsync);
        return endpoints;
    }

    private static async Task<IResult> EmailAsync(
        IServiceProvider services,
        IOptions<EmailOptions> emailOptions,
        CancellationToken cancellationToken)
    {
        SmtpCheckResult result;
        if (emailOptions.Value.UsesBrevoApi())
        {
            var brevoChecker = services.GetRequiredService<BrevoApiConnectivityChecker>();
            result = await brevoChecker.CheckAsync(cancellationToken);
        }
        else
        {
            var smtpChecker = services.GetService<SmtpConnectivityChecker>();
            if (smtpChecker is null)
            {
                return Results.Ok(new
                {
                    status = "ok",
                    provider = "LogOnly",
                    message = "Email is logged to the console in this environment."
                });
            }

            result = await smtpChecker.CheckAsync(cancellationToken);
        }

        if (result.Ok)
        {
            return Results.Ok(new
            {
                status = "ok",
                provider = emailOptions.Value.UsesBrevoApi() ? EmailProviders.BrevoApi : EmailProviders.Smtp,
                host = result.Host,
                port = result.Port,
                fromAddress = result.FromAddress
            });
        }

        return Results.Problem(
            title: "Email check failed",
            detail: result.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable,
            extensions: new Dictionary<string, object?>
            {
                ["provider"] = emailOptions.Value.UsesBrevoApi() ? EmailProviders.BrevoApi : EmailProviders.Smtp,
                ["host"] = result.Host,
                ["port"] = result.Port,
                ["fromAddress"] = result.FromAddress
            });
    }

    private static async Task<IResult> DbInfoAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var userCount = await dbContext.Users.CountAsync(cancellationToken);

        return Results.Ok(new
        {
            status = "ok",
            database = connection.Database,
            host = connection.DataSource,
            userCount
        });
    }

    private static async Task<IResult> ReadyAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? Results.Ok(new { status = "ready" })
            : Results.Problem(
                title: "Database unavailable",
                statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
