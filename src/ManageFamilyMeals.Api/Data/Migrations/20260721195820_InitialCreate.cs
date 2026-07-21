using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ManageFamilyMeals.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CultureCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "meal_categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsFavorite = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "meal_links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TitleEn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TitleAr = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LegacyTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsFavorite = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    PreviewTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PreviewDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PreviewImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    PreviewSiteName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_meal_links_meal_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "meal_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "app_settings",
                columns: new[] { "Id", "CultureCode" },
                values: new object[] { 1, null });

            migrationBuilder.CreateIndex(
                name: "IX_meal_categories_IsDeleted",
                table: "meal_categories",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_meal_links_CategoryId_IsDeleted",
                table: "meal_links",
                columns: new[] { "CategoryId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_meal_links_IsDeleted_DeletedAtUtc",
                table: "meal_links",
                columns: new[] { "IsDeleted", "DeletedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_settings");

            migrationBuilder.DropTable(
                name: "meal_links");

            migrationBuilder.DropTable(
                name: "meal_categories");
        }
    }
}
