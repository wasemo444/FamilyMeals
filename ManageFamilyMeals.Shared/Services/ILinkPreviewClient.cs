using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Shared.Services;

public interface ILinkPreviewClient
{
    Task<LinkPreviewData?> FetchAsync(string url, CancellationToken cancellationToken = default);
}
