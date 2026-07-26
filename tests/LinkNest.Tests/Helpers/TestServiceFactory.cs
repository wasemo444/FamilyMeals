using LinkNest.Api.Data;
using LinkNest.Shared.Services;

namespace LinkNest.Tests.Helpers;

internal static class TestServiceFactory
{
    public static ContentDataService CreateContentDataService(IAppDataStore store, TestCurrentUserContext? userContext = null) =>
        new(store, userContext ?? new TestCurrentUserContext(), new PermissiveOwnershipAuthorization());

    public static EfAppDataStore CreateEfAppDataStore(AppDbContext context, TestCurrentUserContext? userContext = null) =>
        new(context, userContext ?? new TestCurrentUserContext(), new PermissiveOwnershipAuthorization());
}
