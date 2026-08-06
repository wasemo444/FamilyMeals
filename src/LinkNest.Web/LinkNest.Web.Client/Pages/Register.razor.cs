using LinkNest.Shared.Auth;
using LinkNest.Shared.Services;
using LinkNest.Web.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;

namespace LinkNest.Web.Client.Pages;

/// <summary>
/// Registration page that creates a new account through <see cref="IAuthClient"/>.
/// </summary>
/// <remarks>
/// On success, redirects to the login page with query parameters indicating whether email
/// confirmation is required. Uses a full page load on web so auth state is reset cleanly.
/// </remarks>
public partial class Register
{
    [Inject]
    private IAuthClient AuthClient { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IConfiguration Configuration { get; set; } = default!;

    [Inject]
    private IClientAuthMode ClientAuthMode { get; set; } = default!;

    [Inject]
    private ThemeService ThemeService { get; set; } = default!;

    private readonly RegisterRequest _form = new();
    private string? _error;
    private IReadOnlyList<string> _validationErrors = [];
    private bool _submitting;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ThemeService.InitializeAsync();
        }
    }

    private async Task RegisterAsync()
    {
        if (_submitting)
        {
            return;
        }

        _submitting = true;
        _error = null;
        _validationErrors = [];
        StateHasChanged();

        if (!string.Equals(_form.Password, _form.ConfirmPassword, StringComparison.Ordinal))
        {
            _validationErrors = [L["PasswordsDoNotMatch"]];
            _submitting = false;
            return;
        }

        try
        {
            await AuthClient.RegisterAsync(_form);
            var email = Uri.EscapeDataString(_form.Email.Trim());
            var requireConfirmedEmail = Configuration.GetValue("Auth:RequireConfirmedEmail", defaultValue: true);
            var confirmEmailQuery = requireConfirmedEmail ? "&confirmEmail=true" : string.Empty;
            NavigationManager.NavigateTo(
                $"/login?registered=true{confirmEmailQuery}&email={email}",
                forceLoad: !ClientAuthMode.UsesBearerToken);
        }
        catch (AuthValidationException exception)
        {
            _validationErrors = exception.Errors.SelectMany(entry => entry.Value).ToArray();
            _error = AuthValidationMessages.FormatErrors(exception.Errors);
        }
        catch (UnauthorizedAccessException)
        {
            _error = L["RegisterFailed"];
        }
        catch (TaskCanceledException)
        {
            _error = L["RegisterTimeout"];
        }
        catch (HttpRequestException)
        {
            _error = L["RegisterNetworkError"];
        }
        catch (Exception)
        {
            _error = L["RegisterFailed"];
        }
        finally
        {
            _submitting = false;
        }
    }
}
