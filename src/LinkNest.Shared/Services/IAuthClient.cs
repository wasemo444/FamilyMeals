using LinkNest.Shared.Auth;

namespace LinkNest.Shared.Services;

/// <summary>
/// HTTP client abstraction for register, login, logout, and current-user auth API endpoints.
/// </summary>
public interface IAuthClient
{
    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="request">Registration credentials and optional display name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Profile of the newly registered user.</returns>
    /// <exception cref="AuthValidationException">The API returned field validation errors.</exception>
    /// <exception cref="UnauthorizedAccessException">Credentials were rejected.</exception>
    Task<AuthUserInfo> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates an existing user and establishes a session cookie.
    /// </summary>
    /// <param name="request">Login credentials and remember-me preference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Profile of the authenticated user.</returns>
    /// <exception cref="AuthValidationException">The API returned validation or lockout errors.</exception>
    /// <exception cref="UnauthorizedAccessException">Email or password is invalid.</exception>
    Task<AuthUserInfo> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates an existing user and returns a JWT bearer token for mobile clients.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Access token, expiration, and user profile.</returns>
    Task<AuthTokenResponse> LoginWithTokenAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends the current authentication session.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogoutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the currently authenticated user, if any.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User profile when authenticated; otherwise <see langword="null"/>.</returns>
    Task<AuthUserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
