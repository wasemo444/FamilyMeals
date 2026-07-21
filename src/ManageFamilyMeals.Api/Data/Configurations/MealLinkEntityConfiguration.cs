using ManageFamilyMeals.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ManageFamilyMeals.Api.Data.Configurations;

public sealed class MealLinkEntityConfiguration : IEntityTypeConfiguration<MealLinkEntity>
{
    public void Configure(EntityTypeBuilder<MealLinkEntity> builder)
    {
        builder.ToTable("meal_links");

        builder.HasKey(link => link.Id);

        builder.Property(link => link.TitleEn).HasMaxLength(500);
        builder.Property(link => link.TitleAr).HasMaxLength(500);
        builder.Property(link => link.LegacyTitle).HasMaxLength(500);
        builder.Property(link => link.Url).HasMaxLength(2048).IsRequired();
        builder.Property(link => link.Note).HasMaxLength(2000);
        builder.Property(link => link.PreviewTitle).HasMaxLength(500);
        builder.Property(link => link.PreviewDescription).HasMaxLength(2000);
        builder.Property(link => link.PreviewImageUrl).HasMaxLength(2048);
        builder.Property(link => link.PreviewSiteName).HasMaxLength(200);

        builder.Property(link => link.CreatedAtUtc)
            .HasColumnType("timestamptz");

        builder.Property(link => link.DeletedAtUtc)
            .HasColumnType("timestamptz");

        builder.HasOne(link => link.Category)
            .WithMany(category => category.Links)
            .HasForeignKey(link => link.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(link => new { link.CategoryId, link.IsDeleted });
        builder.HasIndex(link => new { link.IsDeleted, link.DeletedAtUtc });
    }
}
