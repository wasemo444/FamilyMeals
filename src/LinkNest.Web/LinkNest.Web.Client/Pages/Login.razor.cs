using Microsoft.AspNetCore.Components;

namespace LinkNest.Web.Client.Pages;

/// <summary>
/// Login page that displays validation messages and submits credentials via an HTML form post.
/// </summary>
/// <remarks>
/// Authentication is handled by <see cref="LinkNest.Web.Endpoints.AccountEndpoints"/> on the
/// Web host so Identity cookies are issued to the browser. Query parameters carry return URLs,
/// registration success, email confirmation, and error codes from redirects.
/// </remarks>
public partial class Login
{
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

    private string? _error;
    private string? _success;

    protected string EmailValue =>
        string.IsNullOrWhiteSpace(RegisteredEmail) ? string.Empty : RegisteredEmail;

    protected bool RememberMeChecked => false;

    protected override void OnParametersSet()
    {
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
}
