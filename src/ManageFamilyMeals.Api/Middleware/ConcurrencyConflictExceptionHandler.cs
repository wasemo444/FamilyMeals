using ManageFamilyMeals.Shared.Services;
using Microsoft.AspNetCore.Diagnostics;

namespace ManageFamilyMeals.Api.Middleware;

public sealed class ConcurrencyConflictExceptionHandler(ILogger<ConcurrencyConflictExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ConcurrencyConflictException conflictException)
        {
            return false;
        }

        logger.LogWarning(conflictException, "Optimistic concurrency conflict detected.");

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        await httpContext.Response.WriteAsJsonAsync(
            new { error = conflictException.Message },
            cancellationToken);

        return true;
    }
}
