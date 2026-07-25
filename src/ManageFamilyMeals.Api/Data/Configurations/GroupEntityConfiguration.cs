using ManageFamilyMeals.Api.Data.Entities;
using ManageFamilyMeals.Api.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ManageFamilyMeals.Api.Data.Configurations;

/// <summary>
/// EF Core fluent configuration for <see cref="GroupEntity"/> including unique invite code index.
/// </summary>
public sealed class GroupEntityConfiguration : IEntityTypeConfiguration<GroupEntity>
{
    public void Configure(EntityTypeBuilder<GroupEntity> builder)
    {
        builder.ToTable("groups");

        builder.HasKey(group => group.Id);

        builder.Property(group => group.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(group => group.InviteCode)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(group => group.InviteCode)
            .IsUnique();

        builder.Property(group => group.CreatedAtUtc)
            .HasColumnType("timestamptz");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(group => group.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
