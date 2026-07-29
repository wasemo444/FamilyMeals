using LinkNest.Shared.Auth;
using LinkNest.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace LinkNest.Web.Client.Pages;

/// <summary>
/// Account and UI preference settings for authenticated users.
/// </summary>
public partial class Settings
{
    [Inject]
    private IAuthClient AuthClient { get; set; } = default!;

    [Inject]
    private IAuthStateNotifier AuthStateNotifier { get; set; } = default!;

    [Inject]
    private ISecureTokenStore SecureTokenStore { get; set; } = default!;

    [Inject]
    private IClientAuthMode ClientAuthMode { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private readonly UpdateProfileRequest _profileForm = new();
    private readonly DeactivateAccountRequest _deactivateForm = new();
    private readonly List<string> _deactivateErrors = [];

    private string _email = string.Empty;
    private string _deactivatePlaceholder = "DEACTIVATE";
    private string? _profileSuccess;
    private string? _profileError;
    private string? _deactivateError;
    private bool _loading = true;
    private bool _savingProfile;
    private bool _deactivating;
    private bool _showDeactivateForm;

    protected override async Task OnInitializedAsync()
    {
        var user = await AuthClient.GetCurrentUserAsync();
        if (user is null)
        {
            NavigationManager.NavigateTo("/login?returnUrl=%2Fsettings");
            return;
        }

        _email = user.Email;
        _profileForm.DisplayName = user.DisplayName ?? user.Email;
        _deactivatePlaceholder = string.IsNullOrWhiteSpace(user.DisplayName) ? "DEACTIVATE" : user.DisplayName;
        _loading = false;
    }

    private async Task SaveProfileAsync()
    {
        _profileSuccess = null;
        _profileError = null;
        _savingProfile = true;

        try
        {
            var updated = await AuthClient.UpdateProfileAsync(_profileForm);
            _profileForm.DisplayName = updated.DisplayName ?? updated.Email;
            _deactivatePlaceholder = string.IsNullOrWhiteSpace(updated.DisplayName) ? "DEACTIVATE" : updated.DisplayName;
            await AuthStateNotifier.NotifySignedInAsync(updated);
            _profileSuccess = L["ProfileUpdated"];
        }
        catch (AuthValidationException exception)
        {
            _profileError = AuthValidationMessages.FormatErrors(exception.Errors);
        }
        catch (Exception)
        {
            _profileError = L["ProfileUpdateFailed"];
        }
        finally
        {
            _savingProfile = false;
        }
    }

    private async Task DeactivateAsync()
    {
        _deactivateError = null;
        _deactivateErrors.Clear();
        _deactivating = true;

        try
        {
            await AuthClient.DeactivateAccountAsync(_deactivateForm);
            await SecureTokenStore.ClearAsync();
            await AuthStateNotifier.NotifySignedOutAsync();
            NavigationManager.NavigateTo("/login?deactivated=true", forceLoad: !ClientAuthMode.UsesBearerToken);
        }
        catch (AuthValidationException exception)
        {
            _deactivateErrors.AddRange(
                AuthValidationMessages.FormatErrors(exception.Errors)
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries));
        }
        catch (Exception)
        {
            _deactivateError = L["DeactivateAccountFailed"];
        }
        finally
        {
            _deactivating = false;
        }
    }

    private void CancelDeactivate()
    {
        _showDeactivateForm = false;
        _deactivateForm.Password = string.Empty;
        _deactivateForm.Confirmation = string.Empty;
        _deactivateError = null;
        _deactivateErrors.Clear();
    }
}
