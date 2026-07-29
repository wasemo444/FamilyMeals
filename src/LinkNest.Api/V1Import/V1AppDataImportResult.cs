namespace LinkNest.Api.V1Import;

/// <summary>
/// Outcome of importing v1 JSON/localStorage data into PostgreSQL for a target user.
/// </summary>
public sealed class V1AppDataImportResult
{
    public int CategoriesImported { get; init; }

    public int CategoriesSkipped { get; init; }

    public int LinksImported { get; init; }

    public int LinksSkipped { get; init; }

    public bool UserNotFound { get; init; }

    public bool InvalidPayload { get; init; }
}
