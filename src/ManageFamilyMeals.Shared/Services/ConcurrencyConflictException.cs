namespace ManageFamilyMeals.Shared.Services;

/// <summary>
/// Thrown when a save or mutation fails because another client modified the same row first.
/// </summary>
/// <remarks>
/// <see cref="ApiMealDataService"/> maps HTTP 409 responses to this exception.
/// Callers should reload data and retry the operation.
/// </remarks>
public sealed class ConcurrencyConflictException : Exception
{
    /// <summary>
    /// Initializes the exception with the default user-facing conflict message.
    /// </summary>
    public ConcurrencyConflictException()
        : base("The record was modified by another request. Reload and retry.")
    {
    }

    /// <summary>
    /// Initializes the exception with a custom message.
    /// </summary>
    /// <param name="message">Description of the conflict.</param>
    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }
}
