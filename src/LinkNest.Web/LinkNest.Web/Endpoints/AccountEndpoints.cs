using LinkNest.Api.Identity;
using LinkNest.Shared.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LinkNest.Web.Endpoints;

/// <summary>
/// Minimal API endpoints for browser-based Identity sign-in, sign-out, and email confirmation.
/// </summary>
/// <remarks>
/// Login and logout use form posts with antiforgery disabled so Blazor pages can submit HTML forms
/// directly. Logout validates same-origin via Origin or Referer headers before signing out.
/// </remarks>
public static class AccountEndpoints
{
    /// <summary>
    /// Maps <c>/account/login</c>, <c>/account/logout</c>, and <c>/account/confirm-email</c> routes.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to extend.</param>
    /// <returns>The same <paramref name="endpoints"/> instance for chaining.</returns>
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/account/login", LoginAsync).DisableAntiforgery();
        endpoints.MapPost("/account/logout", LogoutAsync).DisableAntiforgery();
        endpoints.MapGet("/account/confirm-email", ConfirmEmailAsync);

        return endpoints;
    }

    private static async Task<IResult> ConfirmEmailAsync(
        [FromQuery] Guid userId,
        [FromQuery] string code,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Results.LocalRedirect("/login?error=invalidToken");
        }

        var result = await userManager.ConfirmEmailAsync(user, code);
        if (!result.Succeeded)
        {
            return Results.LocalRedirect("/login?error=invalidToken");
        }

        var email = Uri.EscapeDataString(user.Email ?? string.Empty);
        return Results.LocalRedirect($"/login?confirmed=true&email={email}");
    }

    private static async Task<IResult> LoginAsync(
        [FromForm(Name = "email")] string? email,
        [FromForm(Name = "password")] string? password,
        [FromForm(Name = "rememberMe")] bool? rememberMe,
        [FromForm(Name = "returnUrl")] string? returnUrl,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        var persistLogin = rememberMe ?? false;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return RedirectToLogin(returnUrl, email, error: "required");
        }

        var normalizedEmail = email.Trim();
        var user = await userManager.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            return RedirectToLogin(returnUrl, normalizedEmail, error: "invalid");
        }

        var result = await signInManager.PasswordSignInAsync(
            user.UserName ?? normalizedEmail,
            password,
            persistLogin,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return RedirectToLogin(returnUrl, normalizedEmail, error: "locked");
        }

        if (result.IsNotAllowed || !result.Succeeded)
        {
            return RedirectToLogin(returnUrl, normalizedEmail, error: "invalid");
        }

        var destination = AuthNavigation.GetSafeReturnUrl(returnUrl);
        return Results.LocalRedirect(destination);
    }

    private static async Task<IResult> LogoutAsync(HttpContext httpContext, SignInManager<ApplicationUser> signInManager)
    {
        if (!IsSameOriginFormPost(httpContext.Request))
        {
            return Results.BadRequest();
        }

        await signInManager.SignOutAsync();
        return Results.LocalRedirect("/login");
    }

    private static bool IsSameOriginFormPost(HttpRequest request)
    {
        if (!HttpMethods.IsPost(request.Method))
        {
            return false;
        }

        var origin = request.Headers.Origin.ToString();
        if (!string.IsNullOrWhiteSpace(origin)
            && Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
        {
            return string.Equals(originUri.Host, request.Host.Host, StringComparison.OrdinalIgnoreCase);
        }

        var referer = request.Headers.Referer.ToString();
        if (!string.IsNullOrWhiteSpace(referer)
            && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
        {
            return string.Equals(refererUri.Host, request.Host.Host, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static IResult RedirectToLogin(string? returnUrl, string? email, string error)
    {
        var query = new List<string> { $"error={Uri.EscapeDataString(error)}" };

        var safeReturnUrl = AuthNavigation.GetSafeReturnUrl(returnUrl, fallback: string.Empty);
        if (!string.IsNullOrWhiteSpace(safeReturnUrl) && safeReturnUrl != "/")
        {
            query.Add($"returnUrl={Uri.EscapeDataString(safeReturnUrl)}");
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            query.Add($"email={Uri.EscapeDataString(email.Trim())}");
        }

        return Results.LocalRedirect($"/login?{string.Join('&', query)}");
    }
}
