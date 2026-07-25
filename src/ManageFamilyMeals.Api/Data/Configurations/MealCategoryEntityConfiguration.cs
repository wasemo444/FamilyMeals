using ManageFamilyMeals.Api.Data.Entities;
using ManageFamilyMeals.Api.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ManageFamilyMeals.Api.Data.Configurations;

public sealed class MealCategoryEntityConfiguration : IEntityTypeConfiguration<MealCategoryEntity>
{
    public void Configure(EntityTypeBuilder<MealCategoryEntity> builder)
    {
        builder.ToTable("meal_categories", tableBuilder =>
            tableBuilder.HasCheckConstraint("CK_meal_categories_owner", OwnershipConstraintSql.CategoryOwnerCheck));

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(category => category.CreatedAtUtc)
            .HasColumnType("timestamptz");

        builder.Property(category => category.DeletedAtUtc)
            .HasColumnType("timestamptz");

        builder.Property(category => category.RowVersion)
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(category => category.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<GroupEntity>()
            .WithMany()
            .HasForeignKey(category => category.OwnerGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(category => category.IsDeleted);
        builder.HasIndex(category => new { category.OwnerType, category.OwnerUserId });
        builder.HasIndex(category => new { category.OwnerType, category.OwnerGroupId });
    }
}
