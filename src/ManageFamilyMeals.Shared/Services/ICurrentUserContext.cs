namespace ManageFamilyMeals.Shared.Services;

public interface ICurrentUserContext
{
    Guid? UserId { get; }

    Guid GetRequiredUserId();
}
