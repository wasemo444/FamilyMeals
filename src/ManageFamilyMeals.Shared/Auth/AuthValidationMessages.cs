namespace ManageFamilyMeals.Shared.Auth;

public static class AuthValidationMessages
{
    public static string FormatErrors(IReadOnlyDictionary<string, string[]> errors) =>
        string.Join(Environment.NewLine, errors.SelectMany(entry => entry.Value));
}
