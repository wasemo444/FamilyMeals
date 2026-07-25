using ManageFamilyMeals.Api.Identity;
using ManageFamilyMeals.Shared.Auth;
using ManageFamilyMeals.Shared.Services;
using Microsoft.AspNetCore.Identity;

namespace ManageFamilyMeals.Web.Auth;

/// <summary>
/// Uses Identity sign-in on the Web host when an HTTP context is available so auth
/// cookies are issued to the browser. Falls back to the HTTP API client otherwise.
/// </summary>
public sealed class WebHostAuthClient(
    IHttpContextAccessor httpContextAccessor,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    AuthClient apiAuthClient) : IAuthClient
{
    public Task<AuthUserInfo> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default) =>
        apiAuthClient.RegisterAsync(request, cancellationToken);

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

        var result = await signInManager.PasswordSignInAsync(
            user?.UserName ?? normalizedEmail,
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

        if (result.IsNotAllowed || !result.Succeeded)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        user ??= await userManager.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        return ToAuthUserInfo(user);
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        if (httpContextAccessor.HttpContext is not null)
        {
            await signInManager.SignOutAsync();
            return;
        }

        await apiAuthClient.LogoutAsync(cancellationToken);
    }

    public async Task<AuthUserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        if (httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true)
        {
            var user = await userManager.GetUserAsync(httpContextAccessor.HttpContext.User);
            return user is null ? null : ToAuthUserInfo(user);
        }

        return await apiAuthClient.GetCurrentUserAsync(cancellationToken);
    }

    private static AuthUserInfo ToAuthUserInfo(ApplicationUser user) => new()
    {
        Id = user.Id,
        Email = user.Email ?? user.UserName ?? string.Empty,
        DisplayName = user.DisplayName
    };
}
