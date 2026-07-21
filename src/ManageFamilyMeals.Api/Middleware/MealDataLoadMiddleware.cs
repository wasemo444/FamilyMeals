using ManageFamilyMeals.Api.Data;
using ManageFamilyMeals.Shared.Services;

namespace ManageFamilyMeals.Api.Middleware;

public sealed class MealDataLoadMiddleware(RequestDelegate next)
{
    private static readonly PathString ApiPrefix = "/api";
    private static readonly PathString BootstrapPath = "/api/bootstrap";
    private static readonly PathString LinkPreviewPath = "/api/link-preview";
    private static readonly PathString SettingsPath = "/api/settings";

    public async Task InvokeAsync(HttpContext context, IMealDataService mealDataService)
    {
        var path = context.Request.Path;

        if (path.StartsWithSegments(ApiPrefix)
            && !path.StartsWithSegments(BootstrapPath)
            && !path.StartsWithSegments(LinkPreviewPath)
            && !path.StartsWithSegments(SettingsPath))
        {
            await mealDataService.EnsureLoadedAsync(context.RequestAborted);
        }

        await next(context);
    }
}
