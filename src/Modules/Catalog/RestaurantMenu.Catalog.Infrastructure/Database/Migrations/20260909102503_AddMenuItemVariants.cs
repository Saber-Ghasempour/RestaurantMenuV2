using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace RestaurantMenu.Catalog.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuItemVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_menu_items_restaurant_id_id",
                schema: "catalog",
                table: "menu_items",
                columns: new[] { "restaurant_id", "id" });

            migrationBuilder.CreateTable(
                name: "menu_item_variants",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    restaurant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    price_currency = table.Column<string>(type: "character(3)", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_item_variants", x => x.id);
                    table.CheckConstraint("ck_menu_item_variants_currency_format", "price_currency ~ '^[A-Z]{3}$'");
                    table.CheckConstraint("ck_menu_item_variants_display_order_non_negative", "display_order >= 0");
                    table.CheckConstraint("ck_menu_item_variants_price_non_negative", "price_amount >= 0");
                    table.ForeignKey(
                        name: "FK_menu_item_variants_menu_items_restaurant_id_menu_item_id",
                        columns: x => new { x.restaurant_id, x.menu_item_id },
                        principalSchema: "catalog",
                        principalTable: "menu_items",
                        principalColumns: new[] { "restaurant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_menu_item_variants_restaurant_item_display_order",
                schema: "catalog",
                table: "menu_item_variants",
                columns: new[] { "restaurant_id", "menu_item_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ux_menu_item_variants_active_name",
                schema: "catalog",
                table: "menu_item_variants",
                columns: new[] { "menu_item_id", "name" },
                unique: true,
                filter: "is_deleted = FALSE");

            migrationBuilder.CreateIndex(
                name: "ux_menu_item_variants_one_default",
                schema: "catalog",
                table: "menu_item_variants",
                columns: new[] { "menu_item_id", "is_default" },
                unique: true,
                filter: "is_default = TRUE AND is_deleted = FALSE");

            migrationBuilder.Sql(
                """
                INSERT INTO catalog.menu_item_variants
                    (id, restaurant_id, menu_item_id, name, price_amount,
                     price_currency, display_order, is_default, is_available,
                     created_at_utc, version, is_deleted)
                SELECT gen_random_uuid(), restaurant_id, id, 'Default',
                       price_amount, upper(price_currency), 0, TRUE, TRUE,
                       created_at_utc, 1, FALSE
                FROM catalog.menu_items;
                """);

            migrationBuilder.DropColumn(
                name: "price_amount",
                schema: "catalog",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "price_currency",
                schema: "catalog",
                table: "menu_items");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "price_amount",
                schema: "catalog",
                table: "menu_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "price_currency",
                schema: "catalog",
                table: "menu_items",
                type: "character(3)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE catalog.menu_items AS item
                SET price_amount = variant.price_amount,
                    price_currency = variant.price_currency
                FROM catalog.menu_item_variants AS variant
                WHERE variant.menu_item_id = item.id
                  AND variant.restaurant_id = item.restaurant_id
                  AND variant.is_default = TRUE
                  AND variant.is_deleted = FALSE;
                """);

            migrationBuilder.DropTable(
                name: "menu_item_variants",
                schema: "catalog");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_menu_items_restaurant_id_id",
                schema: "catalog",
                table: "menu_items");
        }
    }
}
