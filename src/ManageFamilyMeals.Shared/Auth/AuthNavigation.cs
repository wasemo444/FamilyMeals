namespace ManageFamilyMeals.Shared.Auth;

public static class AuthNavigation
{
    public static string GetSafeReturnUrl(string? returnUrl, string fallback = "/")
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return fallback;
        }

        if (!returnUrl.StartsWith('/') || returnUrl.StartsWith("//", StringComparison.Ordinal) || returnUrl.Contains('\\'))
        {
            return fallback;
        }

        return returnUrl;
    }
}
