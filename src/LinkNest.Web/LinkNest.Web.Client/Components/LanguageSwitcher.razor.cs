using Microsoft.AspNetCore.Components;

namespace LinkNest.Web.Client.Components;

/// <summary>
/// Toggle buttons for switching between English and Arabic UI cultures.
/// </summary>
public partial class LanguageSwitcher
{
    [Inject]
    private Services.CultureState CultureState { get; set; } = default!;

    private string GetButtonClass(string cultureCode) =>
        CultureState.CultureCode.Equals(cultureCode, StringComparison.OrdinalIgnoreCase)
            ? "lang-btn active"
            : "lang-btn";

    private async Task SetCultureAsync(string cultureCode)
    {
        await CultureService.SetCultureAsync(cultureCode);
    }
}
