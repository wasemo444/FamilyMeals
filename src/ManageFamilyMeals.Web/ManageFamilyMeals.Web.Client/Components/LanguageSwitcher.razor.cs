using Microsoft.AspNetCore.Components;

namespace ManageFamilyMeals.Web.Client.Components;

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
