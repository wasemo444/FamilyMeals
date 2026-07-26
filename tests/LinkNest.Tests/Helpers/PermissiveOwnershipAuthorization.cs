using LinkNest.Shared.Models;
using LinkNest.Shared.Services;

namespace LinkNest.Tests.Helpers;

internal sealed class PermissiveOwnershipAuthorization : IOwnershipAuthorization
{
    public Task<IReadOnlySet<Guid>> GetMemberGroupIdsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());

    public Task ValidateCreateOwnerAsync(ContentOwner owner, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task EnsureCanMutateCategoryAsync(ContentCategory category, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task EnsureCanMutateLinkAsync(SavedLink link, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
