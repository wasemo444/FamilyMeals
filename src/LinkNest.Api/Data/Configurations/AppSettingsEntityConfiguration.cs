using LinkNest.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkNest.Api.Data.Configurations;

/// <summary>
/// EF Core fluent configuration for the singleton <see cref="AppSettingsEntity"/> table and seed row.
/// </summary>
public sealed class AppSettingsEntityConfiguration : IEntityTypeConfiguration<AppSettingsEntity>
{
    /// <summary>Primary key value for the single application settings row.</summary>
    public const int SingletonId = 1;

    public void Configure(EntityTypeBuilder<AppSettingsEntity> builder)
    {
        builder.ToTable("app_settings");

        builder.HasKey(settings => settings.Id);

        builder.Property(settings => settings.CultureCode)
            .HasMaxLength(10);

        builder.HasData(new AppSettingsEntity
        {
            Id = SingletonId,
            CultureCode = null
        });
    }
}
