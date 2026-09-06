using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantMenu.Catalog.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuItems : Migration
    {
        private static readonly string[] RestaurantCategoryOrderColumns =
            ["restaurant_id", "category_id", "display_order"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "menu_items",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    restaurant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    price_currency = table.Column<string>(type: "character(3)", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_menu_items_menu_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "catalog",
                        principalTable: "menu_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_menu_items_category_id",
                schema: "catalog",
                table: "menu_items",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_menu_items_restaurant_category_display_order",
                schema: "catalog",
                table: "menu_items",
                columns: RestaurantCategoryOrderColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "menu_items",
                schema: "catalog");
        }
    }
}
