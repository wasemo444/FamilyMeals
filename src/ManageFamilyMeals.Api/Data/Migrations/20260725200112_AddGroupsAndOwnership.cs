using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManageFamilyMeals.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupsAndOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerGroupId",
                table: "meal_links",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerType",
                table: "meal_links",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "meal_links",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "meal_links",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 });

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerGroupId",
                table: "meal_categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerType",
                table: "meal_categories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "meal_categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "meal_categories",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 });

            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InviteCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_groups_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "group_memberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    JoinedAtUtc = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_memberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_group_memberships_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_group_memberships_groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                UPDATE meal_categories
                SET "OwnerType" = 0,
                    "OwnerUserId" = (
                        SELECT "Id" FROM "AspNetUsers"
                        ORDER BY "CreatedAtUtc" ASC
                        LIMIT 1
                    )
                WHERE "OwnerUserId" IS NULL
                  AND EXISTS (SELECT 1 FROM "AspNetUsers");
                """);

            migrationBuilder.Sql("""
                UPDATE meal_links
                SET "OwnerType" = 0,
                    "OwnerUserId" = (
                        SELECT "Id" FROM "AspNetUsers"
                        ORDER BY "CreatedAtUtc" ASC
                        LIMIT 1
                    )
                WHERE "OwnerUserId" IS NULL
                  AND EXISTS (SELECT 1 FROM "AspNetUsers");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_meal_links_OwnerGroupId",
                table: "meal_links",
                column: "OwnerGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_meal_links_OwnerType_OwnerGroupId",
                table: "meal_links",
                columns: new[] { "OwnerType", "OwnerGroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_meal_links_OwnerType_OwnerUserId",
                table: "meal_links",
                columns: new[] { "OwnerType", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_meal_links_OwnerUserId",
                table: "meal_links",
                column: "OwnerUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_meal_links_owner",
                table: "meal_links",
                sql: "(\"OwnerType\" = 0 AND \"OwnerUserId\" IS NOT NULL AND \"OwnerGroupId\" IS NULL) OR\n(\"OwnerType\" = 1 AND \"OwnerGroupId\" IS NOT NULL AND \"OwnerUserId\" IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_meal_categories_OwnerGroupId",
                table: "meal_categories",
                column: "OwnerGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_meal_categories_OwnerType_OwnerGroupId",
                table: "meal_categories",
                columns: new[] { "OwnerType", "OwnerGroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_meal_categories_OwnerType_OwnerUserId",
                table: "meal_categories",
                columns: new[] { "OwnerType", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_meal_categories_OwnerUserId",
                table: "meal_categories",
                column: "OwnerUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_meal_categories_owner",
                table: "meal_categories",
                sql: "(\"OwnerType\" = 0 AND \"OwnerUserId\" IS NOT NULL AND \"OwnerGroupId\" IS NULL) OR\n(\"OwnerType\" = 1 AND \"OwnerGroupId\" IS NOT NULL AND \"OwnerUserId\" IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_group_memberships_GroupId_UserId",
                table: "group_memberships",
                columns: new[] { "GroupId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_memberships_UserId",
                table: "group_memberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_groups_CreatedByUserId",
                table: "groups",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_groups_InviteCode",
                table: "groups",
                column: "InviteCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_meal_categories_AspNetUsers_OwnerUserId",
                table: "meal_categories",
                column: "OwnerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_meal_categories_groups_OwnerGroupId",
                table: "meal_categories",
                column: "OwnerGroupId",
                principalTable: "groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_meal_links_AspNetUsers_OwnerUserId",
                table: "meal_links",
                column: "OwnerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_meal_links_groups_OwnerGroupId",
                table: "meal_links",
                column: "OwnerGroupId",
                principalTable: "groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_meal_categories_AspNetUsers_OwnerUserId",
                table: "meal_categories");

            migrationBuilder.DropForeignKey(
                name: "FK_meal_categories_groups_OwnerGroupId",
                table: "meal_categories");

            migrationBuilder.DropForeignKey(
                name: "FK_meal_links_AspNetUsers_OwnerUserId",
                table: "meal_links");

            migrationBuilder.DropForeignKey(
                name: "FK_meal_links_groups_OwnerGroupId",
                table: "meal_links");

            migrationBuilder.DropTable(
                name: "group_memberships");

            migrationBuilder.DropTable(
                name: "groups");

            migrationBuilder.DropIndex(
                name: "IX_meal_links_OwnerGroupId",
                table: "meal_links");

            migrationBuilder.DropIndex(
                name: "IX_meal_links_OwnerType_OwnerGroupId",
                table: "meal_links");

            migrationBuilder.DropIndex(
                name: "IX_meal_links_OwnerType_OwnerUserId",
                table: "meal_links");

            migrationBuilder.DropIndex(
                name: "IX_meal_links_OwnerUserId",
                table: "meal_links");

            migrationBuilder.DropCheckConstraint(
                name: "CK_meal_links_owner",
                table: "meal_links");

            migrationBuilder.DropIndex(
                name: "IX_meal_categories_OwnerGroupId",
                table: "meal_categories");

            migrationBuilder.DropIndex(
                name: "IX_meal_categories_OwnerType_OwnerGroupId",
                table: "meal_categories");

            migrationBuilder.DropIndex(
                name: "IX_meal_categories_OwnerType_OwnerUserId",
                table: "meal_categories");

            migrationBuilder.DropIndex(
                name: "IX_meal_categories_OwnerUserId",
                table: "meal_categories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_meal_categories_owner",
                table: "meal_categories");

            migrationBuilder.DropColumn(
                name: "OwnerGroupId",
                table: "meal_links");

            migrationBuilder.DropColumn(
                name: "OwnerType",
                table: "meal_links");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "meal_links");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "meal_links");

            migrationBuilder.DropColumn(
                name: "OwnerGroupId",
                table: "meal_categories");

            migrationBuilder.DropColumn(
                name: "OwnerType",
                table: "meal_categories");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "meal_categories");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "meal_categories");
        }
    }
}
