using System.Security.Claims;
using LinkNest.Api.Identity;
using LinkNest.Shared.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LinkNest.Api.Endpoints;

/// <summary>
/// Minimal API routes for user registration, login, logout, and current-user profile.
/// </summary>
/// <remarks>
/// Registration returns <c>404 Not Found</c> when disabled outside development (not <c>403</c>).
/// Login failures and unauthenticated access to protected routes return <c>401 Unauthorized</c>.
/// Account lockout returns <c>429 Too Many Requests</c>.
/// </remarks>
public static class AuthEndpoints
{
    /// <summary>
    /// Maps <c>/api/auth</c> endpoints with rate limiting applied to the group.
    /// </summary>
    /// <param name="endpoints">The application endpoint builder.</param>
    /// <returns>The same <paramref name="endpoints"/> instance for chaining.</returns>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth")
            .RequireRateLimiting("auth");

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapPost("/token", TokenLoginAsync);
        group.MapPost("/logout", LogoutAsync).RequireAuthorization();
        group.MapGet("/me", GetCurrentUserAsync).RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        UserManager<ApplicationUser> userManager,
        EmailConfirmationService emailConfirmationService,
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

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["ConfirmPassword"] = ["Passwords do not match."]
            });
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            EmailConfirmed = !authOptions.Value.RequireConfirmedEmail,
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

        if (authOptions.Value.RequireConfirmedEmail)
        {
            await emailConfirmationService.SendConfirmationEmailAsync(user);
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

        var normalizedEmail = request.Email.Trim();
        var user = await signInManager.UserManager.FindByEmailAsync(normalizedEmail);

        var result = await signInManager.PasswordSignInAsync(
            user?.UserName ?? normalizedEmail,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return Results.Problem(
                detail: "Account is temporarily locked. Try again later.",
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        if (result.IsNotAllowed || !result.Succeeded)
        {
            return Results.Unauthorized();
        }

        user = await signInManager.UserManager.FindByEmailAsync(normalizedEmail);
        return user is null
            ? Results.Unauthorized()
            : Results.Ok(ToAuthUserInfo(user));
    }

    private static async Task<IResult> TokenLoginAsync(
        LoginRequest request,
        SignInManager<ApplicationUser> signInManager,
        JwtTokenService jwtTokenService)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Email and password are required." });
        }

        var normalizedEmail = request.Email.Trim();
        var user = await signInManager.UserManager.FindByEmailAsync(normalizedEmail);

        var result = await signInManager.CheckPasswordSignInAsync(
            user ?? new ApplicationUser(),
            request.Password,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return Results.Problem(
                detail: "Account is temporarily locked. Try again later.",
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        if (user is null || result.IsNotAllowed || !result.Succeeded)
        {
            return Results.Unauthorized();
        }

        var (accessToken, expiresAtUtc) = jwtTokenService.CreateToken(user);
        return Results.Ok(new AuthTokenResponse
        {
            AccessToken = accessToken,
            ExpiresAtUtc = expiresAtUtc,
            User = ToAuthUserInfo(user)
        });
    }

    /// <remarks>
    /// Clears the cookie authentication session only. Mobile bearer clients should discard
    /// stored JWTs locally; tokens remain valid until expiry unless server-side revocation is added.
    /// </remarks>
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
