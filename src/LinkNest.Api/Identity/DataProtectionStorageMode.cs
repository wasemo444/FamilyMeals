namespace LinkNest.Api.Identity;

/// <summary>
/// Supported persistence backends for ASP.NET Core Data Protection keys.
/// </summary>
public static class DataProtectionStorageMode
{
    /// <summary>Shared filesystem directory (Docker Compose / VM).</summary>
    public const string FileSystem = "FileSystem";

    /// <summary>PostgreSQL via <see cref="Microsoft.AspNetCore.DataProtection.EntityFrameworkCore"/> (Fly.io / multi-app PaaS).</summary>
    public const string Database = "Database";
}
