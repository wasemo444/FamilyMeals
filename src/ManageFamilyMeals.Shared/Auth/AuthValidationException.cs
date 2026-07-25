namespace ManageFamilyMeals.Shared.Auth;

public sealed class AuthValidationException : Exception
{
    public AuthValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base(FormatMessage(errors))
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    private static string FormatMessage(IReadOnlyDictionary<string, string[]> errors) =>
        AuthValidationMessages.FormatErrors(errors);
}
