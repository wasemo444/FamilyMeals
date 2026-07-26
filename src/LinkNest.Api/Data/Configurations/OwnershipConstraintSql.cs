using LinkNest.Api.Data.Entities;
using LinkNest.Api.Identity;
using LinkNest.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkNest.Api.Data.Configurations;

/// <summary>
/// PostgreSQL check constraint expressions ensuring meal rows have consistent ownership columns.
/// </summary>
/// <remarks>
/// <see cref="OwnerType.User"/> (0) requires <c>OwnerUserId</c> set and <c>OwnerGroupId</c> null;
/// <see cref="OwnerType.Group"/> (1) requires the inverse. Applied by category and link entity configurations.
/// </remarks>
public static class OwnershipConstraintSql
{
    /// <summary>SQL predicate for <c>meal_categories</c> ownership consistency (<c>CK_meal_categories_owner</c>).</summary>
    public const string CategoryOwnerCheck = """
        ("OwnerType" = 0 AND "OwnerUserId" IS NOT NULL AND "OwnerGroupId" IS NULL) OR
        ("OwnerType" = 1 AND "OwnerGroupId" IS NOT NULL AND "OwnerUserId" IS NULL)
        """;

    /// <summary>SQL predicate for <c>meal_links</c> ownership consistency (<c>CK_meal_links_owner</c>).</summary>
    public const string LinkOwnerCheck = """
        ("OwnerType" = 0 AND "OwnerUserId" IS NOT NULL AND "OwnerGroupId" IS NULL) OR
        ("OwnerType" = 1 AND "OwnerGroupId" IS NOT NULL AND "OwnerUserId" IS NULL)
        """;
}
