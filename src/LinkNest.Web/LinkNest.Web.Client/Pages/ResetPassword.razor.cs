using LinkNest.Shared.Auth;
using LinkNest.Shared.Services;
using LinkNest.Web.Client.Services;
using Microsoft.AspNetCore.Components;

namespace LinkNest.Web.Client.Pages;

/// <summary>
/// Page for setting a new password from an email reset link.
/// </summary>
public partial class ResetPassword
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

    private readonly ResetPasswordRequest _form = new();
    private readonly List<string> _validationErrors = [];
    private string? _error;
    private bool _submitting;
    private bool _hasValidToken;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ThemeService.InitializeAsync();
        }
    }

    protected override void OnParametersSet()
    {
        _hasValidToken = UserId != Guid.Empty && !string.IsNullOrWhiteSpace(Code);
        _form.UserId = UserId;
        _form.Code = Code ?? string.Empty;
    }

    private async Task SubmitAsync()
    {
        _error = null;
        _validationErrors.Clear();
        _submitting = true;

        try
        {
            await AuthClient.ResetPasswordAsync(_form);
            NavigationManager.NavigateTo("/login?reset=true");
        }
        catch (AuthValidationException exception)
        {
            _validationErrors.AddRange(AuthValidationMessages.FormatErrors(exception.Errors).Split('\n', StringSplitOptions.RemoveEmptyEntries));
        }
        catch (Exception)
        {
            _error = L["ResetPasswordFailed"];
        }
        finally
        {
            _submitting = false;
        }
    }
}
