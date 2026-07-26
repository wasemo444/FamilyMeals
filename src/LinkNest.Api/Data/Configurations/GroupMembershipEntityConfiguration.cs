using LinkNest.Api.Data.Entities;
using LinkNest.Api.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkNest.Api.Data.Configurations;

/// <summary>
/// EF Core fluent configuration for <see cref="GroupMembershipEntity"/> with composite unique index on group and user.
/// </summary>
public sealed class GroupMembershipEntityConfiguration : IEntityTypeConfiguration<GroupMembershipEntity>
{
    public void Configure(EntityTypeBuilder<GroupMembershipEntity> builder)
    {
        builder.ToTable("group_memberships");

        builder.HasKey(membership => membership.Id);

        builder.Property(membership => membership.JoinedAtUtc)
            .HasColumnType("timestamptz");

        builder.HasIndex(membership => new { membership.GroupId, membership.UserId })
            .IsUnique();

        builder.HasOne(membership => membership.Group)
            .WithMany(group => group.Memberships)
            .HasForeignKey(membership => membership.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
