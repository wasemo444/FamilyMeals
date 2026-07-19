using ManageFamilyMeals.Shared.Resources;

namespace ManageFamilyMeals.Web.Client.Services;

public interface ILocalizedText
{
    event Action? Changed;

    string this[string name] { get; }

    string Format(string name, params object[] arguments);
}

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
