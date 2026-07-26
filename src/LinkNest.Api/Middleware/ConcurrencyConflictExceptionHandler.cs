using LinkNest.Shared.Services;
using Microsoft.AspNetCore.Diagnostics;

namespace LinkNest.Api.Middleware;

/// <summary>
/// Maps <see cref="ConcurrencyConflictException"/> to HTTP <c>409 Conflict</c> with a JSON error body.
/// </summary>
/// <remarks>
/// Used for optimistic concurrency failures from <see cref="Data.EfAppDataStore"/> and related services.
/// </remarks>
public sealed class ConcurrencyConflictExceptionHandler(ILogger<ConcurrencyConflictExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc />
    /// <returns><see langword="true"/> when the exception was handled; otherwise <see langword="false"/>.</returns>
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
