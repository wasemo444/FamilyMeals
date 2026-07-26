using LinkNest.Shared.Auth;
using LinkNest.Shared.Services;
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

    [SupplyParameterFromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    [SupplyParameterFromQuery(Name = "registered")]
    public bool Registered { get; set; }

    [SupplyParameterFromQuery(Name = "confirmEmail")]
    public bool ConfirmEmail { get; set; }

    [SupplyParameterFromQuery(Name = "confirmed")]
    public bool EmailConfirmed { get; set; }

    [SupplyParameterFromQuery(Name = "email")]
    public string? RegisteredEmail { get; set; }

    [SupplyParameterFromQuery(Name = "error")]
    public string? Error { get; set; }

    private readonly LoginRequest _form = new();
    private string? _error;
    private string? _success;

    protected bool UsesBearerToken => ClientAuthMode.UsesBearerToken;

    protected string EmailValue =>
        string.IsNullOrWhiteSpace(RegisteredEmail) ? string.Empty : RegisteredEmail;

    protected bool RememberMeChecked => false;

    protected override void OnParametersSet()
    {
        if (UsesBearerToken && !string.IsNullOrWhiteSpace(RegisteredEmail))
        {
            _form.Email = RegisteredEmail;
        }

        _error = Error switch
        {
            "invalid" => L["InvalidCredentials"],
            "unconfirmed" => L["EmailNotConfirmed"],
            "locked" => L["LoginFailed"],
            "required" => L["LoginFailed"],
            "invalidToken" => L["EmailConfirmationFailed"],
            _ => null
        };

        _success = EmailConfirmed
            ? L["EmailConfirmedSuccess"]
            : Registered && ConfirmEmail
                ? L["RegistrationConfirmEmail"]
                : Registered
                    ? L["RegistrationSuccessful"]
                    : null;
    }

    private async Task LoginWithTokenAsync()
    {
        _error = null;

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
