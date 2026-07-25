namespace ManageFamilyMeals.Shared.Auth;

/// <summary>
/// Formats auth validation error dictionaries into user-visible message strings.
/// </summary>
public static class AuthValidationMessages
{
    /// <summary>
    /// Flattens field-keyed validation errors into a newline-separated message.
    /// </summary>
    /// <param name="errors">Dictionary mapping field names to error message arrays.</param>
    /// <returns>All error messages joined with platform newlines.</returns>
    public static string FormatErrors(IReadOnlyDictionary<string, string[]> errors) =>
        string.Join(Environment.NewLine, errors.SelectMany(entry => entry.Value));
}
