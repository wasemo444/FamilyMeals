using System.Globalization;

namespace ManageFamilyMeals.Web.Client.Services;

public sealed class CultureState
{
    private string _cultureCode = "en";

    public event Action? Changed;

    public string CultureCode => _cultureCode;

    public CultureInfo CurrentCulture => CreateCulture(_cultureCode);

    public bool IsRightToLeft => CurrentCulture.TextInfo.IsRightToLeft;

    public void SetCulture(string cultureCode, bool notify = true)
    {
        _cultureCode = cultureCode.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ? "ar" : "en";

        var culture = CurrentCulture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        if (notify)
        {
            Changed?.Invoke();
        }
    }

    private static CultureInfo CreateCulture(string cultureCode) =>
        cultureCode.StartsWith("ar", StringComparison.OrdinalIgnoreCase)
            ? new CultureInfo("ar")
            : new CultureInfo("en");
}
