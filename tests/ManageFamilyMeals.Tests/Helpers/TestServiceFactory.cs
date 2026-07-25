using ManageFamilyMeals.Api.Data;
using ManageFamilyMeals.Shared.Services;

namespace ManageFamilyMeals.Tests.Helpers;

internal static class TestServiceFactory
{
    public static MealDataService CreateMealDataService(IAppDataStore store, TestCurrentUserContext? userContext = null) =>
        new(store, userContext ?? new TestCurrentUserContext(), new PermissiveOwnershipAuthorization());

    public static EfAppDataStore CreateEfAppDataStore(AppDbContext context, TestCurrentUserContext? userContext = null) =>
        new(context, userContext ?? new TestCurrentUserContext(), new PermissiveOwnershipAuthorization());
}
