using ManageFamilyMeals.Shared.Auth;
using ManageFamilyMeals.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace ManageFamilyMeals.Web.Client.Pages;

public partial class Login
{
    [Inject]
    private IAuthClient AuthClient { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    private readonly LoginRequest _form = new();
    private string? _error;

    private async Task LoginAsync()
    {
        _error = null;

        try
        {
            await AuthClient.LoginAsync(_form);
            var destination = AuthNavigation.GetSafeReturnUrl(ReturnUrl);
            NavigationManager.NavigateTo(destination, forceLoad: true);
        }
        catch (UnauthorizedAccessException)
        {
            _error = L["InvalidCredentials"];
        }
        catch (Exception)
        {
            _error = L["LoginFailed"];
        }
    }
}
