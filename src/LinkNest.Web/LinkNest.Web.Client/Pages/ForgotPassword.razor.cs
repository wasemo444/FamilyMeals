using LinkNest.Shared.Auth;
using LinkNest.Shared.Services;
using LinkNest.Web.Client.Services;
using Microsoft.AspNetCore.Components;

namespace LinkNest.Web.Client.Pages;

/// <summary>
/// Page for requesting a password-reset email.
/// </summary>
public partial class ForgotPassword
{
    [Inject]
    private IAuthClient AuthClient { get; set; } = default!;

    [Inject]
    private ThemeService ThemeService { get; set; } = default!;

    private readonly ForgotPasswordRequest _form = new();
    private string? _error;
    private string? _success;
    private bool _submitting;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ThemeService.InitializeAsync();
        }
    }

    private async Task SubmitAsync()
    {
        _error = null;
        _success = null;
        _submitting = true;

        try
        {
            await AuthClient.ForgotPasswordAsync(_form);
            _success = L["ForgotPasswordSent"];
        }
        catch (AuthValidationException exception)
        {
            _error = AuthValidationMessages.FormatErrors(exception.Errors);
        }
        catch (Exception)
        {
            _error = L["ForgotPasswordFailed"];
        }
        finally
        {
            _submitting = false;
        }
    }
}
