using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantMenu.Catalog.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchMenuItemTaxRules : Migration
    {
        private static readonly string[] RestaurantMenuItemColumns =
            ["restaurant_id", "menu_item_id"];
        private static readonly string[] MenuItemTenantKeyColumns =
            ["restaurant_id", "id"];
        private static readonly string[] RestaurantBranchColumns =
            ["restaurant_id", "branch_id"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "branch_menu_item_tax_rules",
                schema: "catalog",
                columns: table => new
                {
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    restaurant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rate_basis_points = table.Column<int>(type: "integer", nullable: false),
                    behavior = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_menu_item_tax_rules", x => new { x.branch_id, x.menu_item_id });
                    table.CheckConstraint("ck_branch_menu_item_tax_rules_behavior", "behavior IN ('Inclusive', 'Exclusive')");
                    table.CheckConstraint("ck_branch_menu_item_tax_rules_rate", "rate_basis_points BETWEEN 0 AND 10000");
                    table.ForeignKey(
                        name: "FK_branch_menu_item_tax_rules_menu_items_restaurant_id_menu_it~",
                        columns: x => new { x.restaurant_id, x.menu_item_id },
                        principalSchema: "catalog",
                        principalTable: "menu_items",
                        principalColumns: MenuItemTenantKeyColumns,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_branch_menu_item_tax_rules_restaurant_branch",
                schema: "catalog",
                table: "branch_menu_item_tax_rules",
                columns: RestaurantBranchColumns);

            migrationBuilder.CreateIndex(
                name: "IX_branch_menu_item_tax_rules_restaurant_id_menu_item_id",
                schema: "catalog",
                table: "branch_menu_item_tax_rules",
                columns: RestaurantMenuItemColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_menu_item_tax_rules",
                schema: "catalog");
        }
    }
}
