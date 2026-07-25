using ManageFamilyMeals.Api.Data;
using ManageFamilyMeals.Shared.Services;

namespace ManageFamilyMeals.Api.Middleware;

/// <summary>
/// Ensures in-memory meal data is loaded before handling most authenticated API routes.
/// </summary>
/// <remarks>
/// Skips bootstrap, link preview, settings, auth, and groups paths which manage their own data access or do not need the full snapshot.
/// </remarks>
public sealed class MealDataLoadMiddleware(RequestDelegate next)
{
    private static readonly PathString ApiPrefix = "/api";
    private static readonly PathString BootstrapPath = "/api/bootstrap";
    private static readonly PathString LinkPreviewPath = "/api/link-preview";
    private static readonly PathString SettingsPath = "/api/settings";

    private static readonly PathString AuthPath = "/api/auth";
    private static readonly PathString GroupsPath = "/api/groups";

    /// <summary>
    /// Invokes the middleware, pre-loading meal data when the request path requires it.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="mealDataService">Scoped meal data service to warm before the endpoint runs.</param>
    /// <returns>A task that completes when the remainder of the pipeline finishes.</returns>
    public async Task InvokeAsync(HttpContext context, IMealDataService mealDataService)
    {
        var path = context.Request.Path;

        if (path.StartsWithSegments(ApiPrefix)
            && !path.StartsWithSegments(BootstrapPath)
            && !path.StartsWithSegments(LinkPreviewPath)
            && !path.StartsWithSegments(SettingsPath)
            && !path.StartsWithSegments(AuthPath)
            && !path.StartsWithSegments(GroupsPath))
        {
            await mealDataService.EnsureLoadedAsync(context.RequestAborted);
        }

        await next(context);
    }
}
