namespace ManageFamilyMeals.Shared.Models;

/// <summary>
/// Value object describing the ownership scope for newly created categories and links.
/// Used by the UI and API to distinguish personal content from group-shared content.
/// </summary>
/// <param name="OwnerType">Whether content belongs to the current user or a group.</param>
/// <param name="OwnerGroupId">Required when <paramref name="OwnerType"/> is <see cref="OwnerType.Group"/>.</param>
public sealed record ContentOwner(OwnerType OwnerType = OwnerType.User, Guid? OwnerGroupId = null)
{
    /// <summary>Ownership scope for the signed-in user's personal content.</summary>
    public static ContentOwner Personal => new(OwnerType.User, null);

    /// <summary>
    /// Creates a group ownership scope for the given group id.
    /// </summary>
    /// <param name="groupId">Non-empty group identifier.</param>
    /// <returns>A <see cref="ContentOwner"/> with <see cref="OwnerType.Group"/>.</returns>
    public static ContentOwner ForGroup(Guid groupId) => new(OwnerType.Group, groupId);

    /// <summary>
    /// Validates API or form input and resolves it to a <see cref="ContentOwner"/>.
    /// </summary>
    /// <param name="ownerType">Requested owner type; <see langword="null"/> defaults to personal.</param>
    /// <param name="ownerGroupId">Group id required when <paramref name="ownerType"/> is <see cref="OwnerType.Group"/>.</param>
    /// <param name="owner">Resolved owner when validation succeeds.</param>
    /// <param name="error">Human-readable validation message when validation fails.</param>
    /// <returns><see langword="true"/> when the combination is valid; otherwise <see langword="false"/>.</returns>
    public static bool TryResolve(OwnerType? ownerType, Guid? ownerGroupId, out ContentOwner owner, out string? error)
    {
        if (ownerType == OwnerType.Group)
        {
            if (ownerGroupId is null || ownerGroupId == Guid.Empty)
            {
                owner = Personal;
                error = "ownerGroupId is required when ownerType is Group.";
                return false;
            }

            owner = ForGroup(ownerGroupId.Value);
            error = null;
            return true;
        }

        owner = Personal;
        error = null;
        return true;
    }
}
