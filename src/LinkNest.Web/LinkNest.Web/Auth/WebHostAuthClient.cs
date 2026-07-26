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
