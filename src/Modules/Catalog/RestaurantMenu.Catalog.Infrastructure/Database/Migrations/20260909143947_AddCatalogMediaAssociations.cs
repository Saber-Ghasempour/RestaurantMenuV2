using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace RestaurantMenu.Catalog.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogMediaAssociations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "image_media_id",
                schema: "catalog",
                table: "menu_categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "menu_item_media",
                schema: "catalog",
                columns: table => new
                {
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    media_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    restaurant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    alt_text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_item_media", x => new { x.menu_item_id, x.media_asset_id });
                    table.ForeignKey(
                        name: "FK_menu_item_media_menu_items_restaurant_id_menu_item_id",
                        columns: x => new { x.restaurant_id, x.menu_item_id },
                        principalSchema: "catalog",
                        principalTable: "menu_items",
                        principalColumns: new[] { "restaurant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_menu_item_media_order",
                schema: "catalog",
                table: "menu_item_media",
                columns: new[] { "restaurant_id", "menu_item_id", "display_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_menu_item_media_primary",
                schema: "catalog",
                table: "menu_item_media",
                columns: new[] { "restaurant_id", "menu_item_id" },
                unique: true,
                filter: "is_primary");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "menu_item_media",
                schema: "catalog");

            migrationBuilder.DropColumn(
                name: "image_media_id",
                schema: "catalog",
                table: "menu_categories");
        }
    }
}
#pragma warning restore CA1861
