using LinkNest.Shared.Auth;
using LinkNest.Shared.Services;
using LinkNest.Web.Client.Services;
using Microsoft.AspNetCore.Components;

namespace LinkNest.Web.Client.Pages;

/// <summary>
/// Confirms a user's email via <c>POST /api/auth/confirm-email</c> (static WASM path).
/// </summary>
public partial class ConfirmEmail
{
    [Inject]
    private IAuthClient AuthClient { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ThemeService ThemeService { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "userId")]
    public Guid UserId { get; set; }

    [SupplyParameterFromQuery(Name = "code")]
    public string? Code { get; set; }

    private string? _error;
    private bool _started;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ThemeService.InitializeAsync();
        }

        if (firstRender && !_started)
        {
            _started = true;
            await ConfirmAsync();
        }
    }

    private async Task ConfirmAsync()
    {
        if (UserId == Guid.Empty || string.IsNullOrWhiteSpace(Code))
        {
            NavigationManager.NavigateTo("/login?error=invalidToken");
            return;
        }

        try
        {
            await AuthClient.ConfirmEmailAsync(new ConfirmEmailRequest
            {
                UserId = UserId,
                Code = Code
            });

            NavigationManager.NavigateTo("/login?confirmed=true");
        }
        catch (AuthValidationException)
        {
            NavigationManager.NavigateTo("/login?error=invalidToken");
        }
        catch (UnauthorizedAccessException)
        {
            NavigationManager.NavigateTo("/login?error=deactivated");
        }
        catch (Exception)
        {
            _error = L["EmailConfirmationFailed"];
            StateHasChanged();
        }
    }
}
