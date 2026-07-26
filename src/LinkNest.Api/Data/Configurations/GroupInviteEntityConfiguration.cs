using LinkNest.Api.Data.Entities;
using LinkNest.Api.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkNest.Api.Data.Configurations;

/// <summary>
/// EF Core fluent configuration for <see cref="GroupInviteEntity"/>.
/// </summary>
public sealed class GroupInviteEntityConfiguration : IEntityTypeConfiguration<GroupInviteEntity>
{
    public void Configure(EntityTypeBuilder<GroupInviteEntity> builder)
    {
        builder.ToTable("group_invites");

        builder.HasKey(invite => invite.Id);

        builder.Property(invite => invite.Status)
            .HasConversion<int>();

        builder.Property(invite => invite.CreatedAtUtc)
            .HasColumnType("timestamptz");

        builder.Property(invite => invite.RespondedAtUtc)
            .HasColumnType("timestamptz");

        builder.HasIndex(invite => new { invite.GroupId, invite.InviteeUserId, invite.Status });

        builder.HasIndex(invite => new { invite.GroupId, invite.InviteeUserId })
            .IsUnique()
            .HasFilter("\"Status\" = 0")
            .HasDatabaseName("IX_group_invites_GroupId_InviteeUserId_pending_unique");

        builder.HasOne(invite => invite.Group)
            .WithMany(group => group.Invites)
            .HasForeignKey(invite => invite.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(invite => invite.InviteeUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(invite => invite.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
