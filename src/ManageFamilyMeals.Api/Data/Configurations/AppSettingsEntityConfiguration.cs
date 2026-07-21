using ManageFamilyMeals.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ManageFamilyMeals.Api.Data.Configurations;

public sealed class AppSettingsEntityConfiguration : IEntityTypeConfiguration<AppSettingsEntity>
{
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
