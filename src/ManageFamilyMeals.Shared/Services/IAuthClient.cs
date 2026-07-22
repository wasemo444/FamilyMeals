using ManageFamilyMeals.Shared.Auth;

namespace ManageFamilyMeals.Shared.Services;

public interface IAuthClient
{
    Task<AuthUserInfo> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<AuthUserInfo> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);

    Task<AuthUserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
