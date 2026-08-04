using System.Security.Claims;
using LinkNest.Api.Identity;
using LinkNest.Shared.Auth;
using LinkNest.Shared.Services;
using Microsoft.AspNetCore.Identity;

namespace LinkNest.Web.Auth;
/// <summary>
/// <see cref="IAuthClient"/> implementation that signs in through ASP.NET Core Identity on the Web host.
/// </summary>
/// <remarks>
/// When an HTTP context is available, login and logout mutate Identity cookies on the Web host.
/// Registration and other operations without a browser context delegate to the shared
/// <see cref="AuthClient"/> API client. Used during interactive server rendering and form-based login.
/// </remarks>
public sealed class WebHostAuthClient(
    IHttpContextAccessor httpContextAccessor,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    AuthClient apiAuthClient) : IAuthClient
{
    /// <inheritdoc />
    public Task<AuthUserInfo> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default) =>
        apiAuthClient.RegisterAsync(request, cancellationToken);

    /// <inheritdoc />
    public async Task<AuthUserInfo> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (httpContextAccessor.HttpContext is null)
        {
            return await apiAuthClient.LoginAsync(request, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new AuthValidationException(new Dictionary<string, string[]>
            {
                ["error"] = ["Email and password are required."]
            });
        }

        var normalizedEmail = request.Email.Trim();
        var user = await userManager.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            await signInManager.PasswordSignInAsync(normalizedEmail, request.Password, request.RememberMe, lockoutOnFailure: true);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("This account has been deactivated.");
        }

        var result = await signInManager.PasswordSignInAsync(
            user.UserName ?? normalizedEmail,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            throw new AuthValidationException(new Dictionary<string, string[]>
            {
                ["error"] = ["Account is temporarily locked. Try again later."]
            });
        }

        if (result.IsNotAllowed)
        {
            throw new UnauthorizedAccessException("Please confirm your email address before signing in.");
        }

        if (!result.Succeeded)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        return ToAuthUserInfo(user);
    }

    /// <inheritdoc />
    public Task<AuthTokenResponse> LoginWithTokenAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
        apiAuthClient.LoginWithTokenAsync(request, cancellationToken);

    /// <inheritdoc />
    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        if (httpContextAccessor.HttpContext is not null)
        {
            await signInManager.SignOutAsync();
            return;
        }

        await apiAuthClient.LogoutAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<AuthUserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User.Identity?.IsAuthenticated == true)
        {
            return Task.FromResult(ToAuthUserInfoFromClaims(httpContext.User));
        }

        return apiAuthClient.GetCurrentUserAsync(cancellationToken);
    }
    /// <inheritdoc />
    public Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default) =>
        apiAuthClient.ForgotPasswordAsync(request, cancellationToken);

    /// <inheritdoc />
    public Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default) =>
        apiAuthClient.ResetPasswordAsync(request, cancellationToken);

    /// <inheritdoc />
    public Task ResendConfirmationAsync(ResendConfirmationRequest request, CancellationToken cancellationToken = default) =>
        apiAuthClient.ResendConfirmationAsync(request, cancellationToken);

    /// <inheritdoc />
    public Task ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default) =>
        apiAuthClient.ConfirmEmailAsync(request, cancellationToken);

    /// <inheritdoc />
    public Task<AuthUserInfo> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default) =>
        apiAuthClient.UpdateProfileAsync(request, cancellationToken);

    /// <inheritdoc />
    public Task DeactivateAccountAsync(DeactivateAccountRequest request, CancellationToken cancellationToken = default) =>
        apiAuthClient.DeactivateAccountAsync(request, cancellationToken);

    private static AuthUserInfo? ToAuthUserInfoFromClaims(ClaimsPrincipal principal)
    {
        var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(idValue, out var userId))
        {
            return null;
        }

        var email = principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue(ClaimTypes.Name)
            ?? string.Empty;

        return new AuthUserInfo
        {
            Id = userId,
            Email = email,
            DisplayName = principal.FindFirstValue("DisplayName")
                ?? principal.FindFirstValue(ClaimTypes.Name)
                ?? email
        };
    }

    private static AuthUserInfo ToAuthUserInfo(ApplicationUser user) => new()
    {
        Id = user.Id,
        Email = user.Email ?? user.UserName ?? string.Empty,
        DisplayName = user.DisplayName
    };
}
