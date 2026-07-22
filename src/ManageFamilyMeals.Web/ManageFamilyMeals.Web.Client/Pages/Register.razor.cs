using ManageFamilyMeals.Shared.Auth;
using ManageFamilyMeals.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace ManageFamilyMeals.Web.Client.Pages;

public partial class Register
{
    [Inject]
    private IAuthClient AuthClient { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    private readonly RegisterRequest _form = new();
    private string? _error;

    private async Task RegisterAsync()
    {
        _error = null;

        try
        {
            await AuthClient.RegisterAsync(_form);
            NavigationManager.NavigateTo("/login", forceLoad: true);
        }
        catch (HttpRequestException exception)
        {
            _error = exception.Message;
        }
        catch (Exception)
        {
            _error = L["RegisterFailed"];
        }
    }
}
