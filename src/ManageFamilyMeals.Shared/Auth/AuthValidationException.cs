namespace ManageFamilyMeals.Shared.Auth;

/// <summary>
/// Thrown by <see cref="Services.AuthClient"/> when the auth API returns validation or lockout errors
/// that should be displayed field-by-field in the login or registration UI.
/// </summary>
public sealed class AuthValidationException : Exception
{
    /// <summary>
    /// Initializes the exception with field-keyed validation messages from the API response.
    /// </summary>
    /// <param name="errors">Dictionary mapping field names to one or more error messages.</param>
    public AuthValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base(FormatMessage(errors))
    {
        Errors = errors;
    }

    /// <summary>Field-keyed validation messages suitable for binding to form errors.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    private static string FormatMessage(IReadOnlyDictionary<string, string[]> errors) =>
        AuthValidationMessages.FormatErrors(errors);
}
