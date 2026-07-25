using ManageFamilyMeals.Api.Data.Entities;
using ManageFamilyMeals.Api.Identity;
using ManageFamilyMeals.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ManageFamilyMeals.Api.Data.Configurations;

public static class OwnershipConstraintSql
{
    public const string CategoryOwnerCheck = """
        ("OwnerType" = 0 AND "OwnerUserId" IS NOT NULL AND "OwnerGroupId" IS NULL) OR
        ("OwnerType" = 1 AND "OwnerGroupId" IS NOT NULL AND "OwnerUserId" IS NULL)
        """;

    public const string LinkOwnerCheck = """
        ("OwnerType" = 0 AND "OwnerUserId" IS NOT NULL AND "OwnerGroupId" IS NULL) OR
        ("OwnerType" = 1 AND "OwnerGroupId" IS NOT NULL AND "OwnerUserId" IS NULL)
        """;
}
