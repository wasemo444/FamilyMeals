using LinkNest.Shared.Auth;
using LinkNest.Shared.Services;
using LinkNest.Web.Client.Services;
using Microsoft.AspNetCore.Components;

namespace LinkNest.Web.Client.Pages;

/// <summary>
/// Login page that supports cookie form posts (web) and JWT bearer login (mobile).
/// </summary>
public partial class Login
{
    [Inject]
    private IClientAuthMode ClientAuthMode { get; set; } = default!;

    [Inject]
    private IAuthClient AuthClient { get; set; } = default!;

    [Inject]
    private ISecureTokenStore SecureTokenStore { get; set; } = default!;

    [Inject]
    private IAuthStateNotifier AuthStateNotifier { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ThemeService ThemeService { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    [SupplyParameterFromQuery(Name = "registered")]
    public bool Registered { get; set; }

    [SupplyParameterFromQuery(Name = "confirmEmail")]
    public bool ConfirmEmail { get; set; }

    [SupplyParameterFromQuery(Name = "confirmed")]
    public bool EmailConfirmed { get; set; }

    [SupplyParameterFromQuery(Name = "reset")]
    public bool PasswordReset { get; set; }

    [SupplyParameterFromQuery(Name = "deactivated")]
    public bool AccountDeactivated { get; set; }

    [SupplyParameterFromQuery(Name = "email")]
    public string? RegisteredEmail { get; set; }

    [SupplyParameterFromQuery(Name = "error")]
    public string? Error { get; set; }

    private readonly LoginRequest _form = new();
    private string? _error;
    private string? _success;
    private string? _resendSuccess;
    private bool _showResendConfirmation;
    private bool _resending;
    private bool _submitting;

    protected bool UsesBearerToken => ClientAuthMode.UsesBearerToken;

    protected string EmailValue =>
        string.IsNullOrWhiteSpace(RegisteredEmail) ? string.Empty : RegisteredEmail;

    protected bool RememberMeChecked => false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ThemeService.InitializeAsync();
        }
    }

    protected override void OnParametersSet()
    {
        if (UsesBearerToken && !string.IsNullOrWhiteSpace(RegisteredEmail))
        {
            _form.Email = RegisteredEmail;
        }

        _showResendConfirmation = Error == "unconfirmed";

        _error = Error switch
        {
            "invalid" => L["InvalidCredentials"],
            "unconfirmed" => L["EmailNotConfirmed"],
            "deactivated" => L["AccountDeactivated"],
            "locked" => L["LoginFailed"],
            "required" => L["LoginFailed"],
            "invalidToken" => L["EmailConfirmationFailed"],
            _ => null
        };

        _success = EmailConfirmed
            ? L["EmailConfirmedSuccess"]
            : PasswordReset
                ? L["PasswordResetSuccess"]
                : AccountDeactivated
                    ? L["AccountDeactivatedSuccess"]
                    : Registered && ConfirmEmail
                        ? L["RegistrationConfirmEmail"]
                        : Registered
                            ? L["RegistrationSuccessful"]
                            : null;
    }

    private async Task LoginWithTokenAsync()
    {
        if (_submitting)
        {
            return;
        }

        _submitting = true;
        _error = null;
        _resendSuccess = null;
        _showResendConfirmation = false;

        try
        {
            var response = await AuthClient.LoginWithTokenAsync(_form);
            await SecureTokenStore.SaveAsync(response.AccessToken, response.ExpiresAtUtc);
            await AuthStateNotifier.NotifySignedInAsync(response.User);

            var destination = string.IsNullOrWhiteSpace(ReturnUrl) ? "/" : ReturnUrl;
            NavigationManager.NavigateTo(destination);
        }
        catch (AuthValidationException exception)
        {
            _error = AuthValidationMessages.FormatErrors(exception.Errors);
        }
        catch (UnauthorizedAccessException exception)
        {
            _error = exception.Message.Contains("confirm", StringComparison.OrdinalIgnoreCase)
                ? L["EmailNotConfirmed"]
                : exception.Message.Contains("deactivated", StringComparison.OrdinalIgnoreCase)
                    ? L["AccountDeactivated"]
                    : L["InvalidCredentials"];
            _showResendConfirmation = _error == L["EmailNotConfirmed"];
        }
        catch (Exception)
        {
            _error = L["LoginFailed"];
        }
        finally
        {
            _submitting = false;
        }
    }

    private async Task ResendConfirmationAsync()
    {
        _resendSuccess = null;
        _resending = true;

        var email = UsesBearerToken ? _form.Email : EmailValue;
        if (string.IsNullOrWhiteSpace(email))
        {
            _error = L["ResendConfirmationEmailRequired"];
            _resending = false;
            return;
        }

        try
        {
            await AuthClient.ResendConfirmationAsync(new ResendConfirmationRequest { Email = email.Trim() });
            _resendSuccess = L["ResendConfirmationSent"];
        }
        catch (Exception)
        {
            _error = L["ResendConfirmationFailed"];
        }
        finally
        {
            _resending = false;
        }
    }
}
