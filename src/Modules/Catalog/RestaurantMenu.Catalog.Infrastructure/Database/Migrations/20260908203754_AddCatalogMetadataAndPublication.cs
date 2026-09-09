using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantMenu.Catalog.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogMetadataAndPublication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "allergen_notes",
                schema: "catalog",
                table: "menu_items",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "calories",
                schema: "catalog",
                table: "menu_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_featured",
                schema: "catalog",
                table: "menu_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_published",
                schema: "catalog",
                table: "menu_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<short>(
                name: "preparation_time_minutes",
                schema: "catalog",
                table: "menu_items",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recipe",
                schema: "catalog",
                table: "menu_items",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "tags",
                schema: "catalog",
                table: "menu_items",
                type: "text[]",
                nullable: false,
                defaultValueSql: "ARRAY[]::text[]");

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "catalog",
                table: "menu_categories",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_published",
                schema: "catalog",
                table: "menu_categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Preserve the visibility of content created before explicit draft/publication state existed.
            // New rows continue to use the false database and aggregate default.
            migrationBuilder.Sql(
                "UPDATE catalog.menu_categories SET is_published = TRUE;");
            migrationBuilder.Sql(
                "UPDATE catalog.menu_items SET is_published = TRUE;");

            migrationBuilder.CreateIndex(
                name: "ix_menu_items_tags_gin",
                schema: "catalog",
                table: "menu_items",
                column: "tags")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.AddCheckConstraint(
                name: "ck_menu_items_calories_non_negative",
                schema: "catalog",
                table: "menu_items",
                sql: "calories IS NULL OR calories >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_menu_items_preparation_time_minutes_range",
                schema: "catalog",
                table: "menu_items",
                sql: "preparation_time_minutes IS NULL OR (preparation_time_minutes >= 1 AND preparation_time_minutes <= 1440)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_menu_items_tags_gin",
                schema: "catalog",
                table: "menu_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_menu_items_calories_non_negative",
                schema: "catalog",
                table: "menu_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_menu_items_preparation_time_minutes_range",
                schema: "catalog",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "allergen_notes",
                schema: "catalog",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "calories",
                schema: "catalog",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "is_featured",
                schema: "catalog",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "is_published",
                schema: "catalog",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "preparation_time_minutes",
                schema: "catalog",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "recipe",
                schema: "catalog",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "tags",
                schema: "catalog",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "catalog",
                table: "menu_categories");

            migrationBuilder.DropColumn(
                name: "is_published",
                schema: "catalog",
                table: "menu_categories");
        }
    }
}
