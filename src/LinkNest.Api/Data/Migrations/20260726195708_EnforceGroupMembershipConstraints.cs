using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinkNest.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnforceGroupMembershipConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_group_memberships_UserId",
                table: "group_memberships");

            migrationBuilder.CreateIndex(
                name: "IX_group_memberships_UserId_unique",
                table: "group_memberships",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_invites_GroupId_InviteeUserId_pending_unique",
                table: "group_invites",
                columns: new[] { "GroupId", "InviteeUserId" },
                unique: true,
                filter: "\"Status\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_group_memberships_UserId_unique",
                table: "group_memberships");

            migrationBuilder.DropIndex(
                name: "IX_group_invites_GroupId_InviteeUserId_pending_unique",
                table: "group_invites");

            migrationBuilder.CreateIndex(
                name: "IX_group_memberships_UserId",
                table: "group_memberships",
                column: "UserId");
        }
    }
}
