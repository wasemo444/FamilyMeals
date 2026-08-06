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
        group.MapPatch("/me", UpdateProfileAsync).RequireAuthorization();
        group.MapPost("/forgot-password", ForgotPasswordAsync);
        group.MapPost("/reset-password", ResetPasswordAsync);
        group.MapPost("/resend-confirmation", ResendConfirmationAsync);
        group.MapPost("/confirm-email", ConfirmEmailAsync);
        group.MapPost("/deactivate", DeactivateAccountAsync).RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        UserManager<ApplicationUser> userManager,
        EmailConfirmationService emailConfirmationService,
        IOptions<AuthOptions> authOptions,
        IHostEnvironment environment,
        ILoggerFactory loggerFactory)
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
            var logger = loggerFactory.CreateLogger("LinkNest.Auth.Register");
            var existing = await userManager.FindByEmailAsync(user.Email!);
            if (existing is not null)
            {
                logger.LogWarning(
                    "Registration rejected for {Email}: duplicate. Existing user {UserId}, EmailConfirmed={EmailConfirmed}.",
                    user.Email,
                    existing.Id,
                    existing.EmailConfirmed);
            }

            return Results.ValidationProblem(result.Errors.ToDictionary(
                error => error.Code,
                error => new[] { error.Description }));
        }

        if (authOptions.Value.RequireConfirmedEmail)
        {
            try
            {
                await emailConfirmationService.SendConfirmationEmailAsync(user);
            }
            catch (Exception ex)
            {
                var logger = loggerFactory.CreateLogger("LinkNest.Auth.Register");
                logger.LogError(
                    ex,
                    "Registration rolled back for {Email}: confirmation email could not be sent.",
                    user.Email);

                await userManager.DeleteAsync(user);
                var detail = ex switch
                {
                    InvalidOperationException operationException =>
                        $"Account was not created because the confirmation email could not be sent. {operationException.Message}",
                    _ =>
                        "Account was not created because the confirmation email could not be sent. Check API email settings and try again."
                };
                return Results.Problem(
                    title: "Email delivery failed",
                    detail: detail,
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
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

        var loginFailure = EvaluateLoginEligibility(user);
        if (loginFailure is not null)
        {
            return loginFailure;
        }

        var result = await signInManager.PasswordSignInAsync(
            user!.UserName ?? normalizedEmail,
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
            return Results.Problem(
                detail: "Please confirm your email address before signing in.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Email not confirmed");
        }

        if (!result.Succeeded)
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

        var loginFailure = EvaluateLoginEligibility(user);
        if (loginFailure is not null)
        {
            return loginFailure;
        }

        var result = await signInManager.CheckPasswordSignInAsync(
            user!,
            request.Password,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return Results.Problem(
                detail: "Account is temporarily locked. Try again later.",
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        if (result.IsNotAllowed)
        {
            return Results.Problem(
                detail: "Please confirm your email address before signing in.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Email not confirmed");
        }

        if (!result.Succeeded)
        {
            return Results.Unauthorized();
        }

        var (accessToken, expiresAtUtc) = jwtTokenService.CreateToken(user!);
        return Results.Ok(new AuthTokenResponse
        {
            AccessToken = accessToken,
            ExpiresAtUtc = expiresAtUtc,
            User = ToAuthUserInfo(user!)
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
        return user is null || !user.IsActive ? Results.Unauthorized() : Results.Ok(ToAuthUserInfo(user));
    }

    private static async Task<IResult> UpdateProfileAsync(
        UpdateProfileRequest request,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["DisplayName"] = ["Display name is required."]
            });
        }

        var user = await userManager.GetUserAsync(principal);
        if (user is null || !user.IsActive)
        {
            return Results.Unauthorized();
        }

        user.DisplayName = request.DisplayName.Trim();
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return Results.ValidationProblem(result.Errors.ToDictionary(
                error => error.Code,
                error => new[] { error.Description }));
        }

        await signInManager.RefreshSignInAsync(user);
        return Results.Ok(ToAuthUserInfo(user));
    }

    private static async Task<IResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        PasswordResetService passwordResetService)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.BadRequest(new { error = "Email is required." });
        }

        await passwordResetService.SendResetEmailAsync(request.Email);
        return Results.Ok(new { message = "If an account exists for that email, a reset link has been sent." });
    }

    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        PasswordResetService passwordResetService)
    {
        if (request.UserId == Guid.Empty
            || string.IsNullOrWhiteSpace(request.Code)
            || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "User id, code, and password are required." });
        }

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["ConfirmPassword"] = ["Passwords do not match."]
            });
        }

        var result = await passwordResetService.ResetPasswordAsync(
            request.UserId,
            request.Code,
            request.Password);

        if (!result.Succeeded)
        {
            return Results.ValidationProblem(result.Errors.ToDictionary(
                error => error.Code,
                error => new[] { error.Description }));
        }

        return Results.Ok(new { message = "Password has been reset." });
    }

    private static async Task<IResult> ResendConfirmationAsync(
        ResendConfirmationRequest request,
        EmailConfirmationService emailConfirmationService)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.BadRequest(new { error = "Email is required." });
        }

        await emailConfirmationService.ResendConfirmationEmailAsync(request.Email);
        return Results.Ok(new { message = "If an unconfirmed account exists for that email, a confirmation link has been sent." });
    }

    private static async Task<IResult> ConfirmEmailAsync(
        ConfirmEmailRequest request,
        UserManager<ApplicationUser> userManager)
    {
        if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.BadRequest(new { error = "User id and code are required." });
        }

        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Code"] = ["Confirmation link is invalid or expired."]
            });
        }

        if (!user.IsActive)
        {
            return Results.Problem(
                detail: "This account has been deactivated.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Account deactivated");
        }

        var result = await userManager.ConfirmEmailAsync(user, request.Code);
        if (!result.Succeeded)
        {
            return Results.ValidationProblem(result.Errors.ToDictionary(
                error => error.Code,
                error => new[] { error.Description }));
        }

        return Results.Ok(new { message = "Email confirmed." });
    }

    private static async Task<IResult> DeactivateAccountAsync(
        DeactivateAccountRequest request,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null || !user.IsActive)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Password"] = ["Password is required."]
            });
        }

        var expectedConfirmation = user.DisplayName?.Trim() ?? "DEACTIVATE";
        if (!string.Equals(request.Confirmation.Trim(), expectedConfirmation, StringComparison.Ordinal)
            && !string.Equals(request.Confirmation.Trim(), "DEACTIVATE", StringComparison.Ordinal))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Confirmation"] = ["Type your display name or DEACTIVATE to confirm."]
            });
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Password"] = ["Password is incorrect."]
            });
        }

        user.IsActive = false;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Results.ValidationProblem(updateResult.Errors.ToDictionary(
                error => error.Code,
                error => new[] { error.Description }));
        }

        await userManager.UpdateSecurityStampAsync(user);
        await signInManager.SignOutAsync();
        return Results.Ok(new { message = "Account deactivated." });
    }

    private static IResult? EvaluateLoginEligibility(ApplicationUser? user)
    {
        if (user is null)
        {
            return Results.Unauthorized();
        }

        if (!user.IsActive)
        {
            return Results.Problem(
                detail: "This account has been deactivated.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Account deactivated");
        }

        return null;
    }

    private static AuthUserInfo ToAuthUserInfo(ApplicationUser user) => new()
    {
        Id = user.Id,
        Email = user.Email ?? user.UserName ?? string.Empty,
        DisplayName = user.DisplayName
    };
}
