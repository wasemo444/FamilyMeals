using System.Security.Claims;
using ManageFamilyMeals.Api.Identity;
using ManageFamilyMeals.Shared.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ManageFamilyMeals.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth")
            .RequireRateLimiting("auth");

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapPost("/logout", LogoutAsync).RequireAuthorization();
        group.MapGet("/me", GetCurrentUserAsync).RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        UserManager<ApplicationUser> userManager,
        IOptions<AuthOptions> authOptions,
        IHostEnvironment environment)
    {
        if (!authOptions.Value.AllowRegistration && !environment.IsDevelopment())
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Email and password are required." });
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            EmailConfirmed = true,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
                ? request.Email.Trim()
                : request.DisplayName.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return Results.ValidationProblem(result.Errors.ToDictionary(
                error => error.Code,
                error => new[] { error.Description }));
        }

        return Results.Created(
            $"/api/auth/me",
            ToAuthUserInfo(user));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        SignInManager<ApplicationUser> signInManager)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Email and password are required." });
        }

        var result = await signInManager.PasswordSignInAsync(
            request.Email.Trim(),
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return Results.Problem(
                detail: "Account is temporarily locked. Try again later.",
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        if (result.IsNotAllowed)
        {
            return Results.Unauthorized();
        }

        if (!result.Succeeded)
        {
            return Results.Unauthorized();
        }

        var user = await signInManager.UserManager.FindByEmailAsync(request.Email.Trim());
        return user is null
            ? Results.Unauthorized()
            : Results.Ok(ToAuthUserInfo(user));
    }

    private static async Task<IResult> LogoutAsync(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.GetUserAsync(principal);
        return user is null ? Results.Unauthorized() : Results.Ok(ToAuthUserInfo(user));
    }

    private static AuthUserInfo ToAuthUserInfo(ApplicationUser user) => new()
    {
        Id = user.Id,
        Email = user.Email ?? user.UserName ?? string.Empty,
        DisplayName = user.DisplayName
    };
}
