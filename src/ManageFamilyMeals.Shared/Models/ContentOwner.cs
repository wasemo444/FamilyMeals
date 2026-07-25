namespace ManageFamilyMeals.Shared.Models;

public sealed record ContentOwner(OwnerType OwnerType = OwnerType.User, Guid? OwnerGroupId = null)
{
    public static ContentOwner Personal => new(OwnerType.User, null);

    public static ContentOwner ForGroup(Guid groupId) => new(OwnerType.Group, groupId);

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
