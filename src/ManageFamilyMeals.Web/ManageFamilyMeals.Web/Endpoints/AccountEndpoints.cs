using ManageFamilyMeals.Api.Identity;
using ManageFamilyMeals.Shared.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ManageFamilyMeals.Web.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/account/login", LoginAsync).DisableAntiforgery();
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
