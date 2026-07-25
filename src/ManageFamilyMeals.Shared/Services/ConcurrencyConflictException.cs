namespace ManageFamilyMeals.Shared.Services;

public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException()
        : base("The record was modified by another request. Reload and retry.")
    {
    }

    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }
}
