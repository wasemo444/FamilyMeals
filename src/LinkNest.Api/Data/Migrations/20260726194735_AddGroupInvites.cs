using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinkNest.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupInvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "group_invites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    InviteeUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    RespondedAtUtc = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_invites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_group_invites_AspNetUsers_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_group_invites_AspNetUsers_InviteeUserId",
                        column: x => x.InviteeUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_group_invites_groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_group_invites_GroupId_InviteeUserId_Status",
                table: "group_invites",
                columns: new[] { "GroupId", "InviteeUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_group_invites_InvitedByUserId",
                table: "group_invites",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_group_invites_InviteeUserId",
                table: "group_invites",
                column: "InviteeUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "group_invites");
        }
    }
}
