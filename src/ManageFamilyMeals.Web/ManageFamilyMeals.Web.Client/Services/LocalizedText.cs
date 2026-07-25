using ManageFamilyMeals.Shared.Resources;

namespace ManageFamilyMeals.Web.Client.Services;

/// <summary>
/// Provides localized strings keyed by resource name for the current culture.
/// </summary>
public interface ILocalizedText
{
    event Action? Changed;

    string this[string name] { get; }

    string Format(string name, params object[] arguments);
}

/// <summary>
/// Resolves strings from <see cref="LocalizationCatalog"/> using the active <see cref="CultureState"/>.
/// </summary>
public sealed class LocalizedText : ILocalizedText
{
    private readonly CultureState _cultureState;

    public LocalizedText(CultureState cultureState)
    {
        _cultureState = cultureState;
        _cultureState.Changed += () => Changed?.Invoke();
    }

    public event Action? Changed;

    public string this[string name] => LocalizationCatalog.Get(_cultureState.CultureCode, name);

    public string Format(string name, params object[] arguments) =>
        LocalizationCatalog.Format(_cultureState.CultureCode, name, arguments);
}
