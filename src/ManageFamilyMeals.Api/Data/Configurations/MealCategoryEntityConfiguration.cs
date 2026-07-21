using ManageFamilyMeals.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ManageFamilyMeals.Api.Data.Configurations;

public sealed class MealCategoryEntityConfiguration : IEntityTypeConfiguration<MealCategoryEntity>
{
    public void Configure(EntityTypeBuilder<MealCategoryEntity> builder)
    {
        builder.ToTable("meal_categories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(category => category.CreatedAtUtc)
            .HasColumnType("timestamptz");

        builder.Property(category => category.DeletedAtUtc)
            .HasColumnType("timestamptz");

        builder.HasIndex(category => category.IsDeleted);
    }
}
