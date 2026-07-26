using LinkNest.Shared.Models;
using LinkNest.Shared.Services;

namespace LinkNest.Tests.Helpers;

internal sealed class InMemoryAppDataStore : IAppDataStore
{
    private AppData? _data;

    public Task<AppData?> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_data);

    public Task SaveAsync(AppData data, CancellationToken cancellationToken = default)
    {
        _data = data;
        return Task.CompletedTask;
    }
}
