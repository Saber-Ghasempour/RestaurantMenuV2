using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantMenu.Catalog.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchCategoryPublications : Migration
    {
        private static readonly string[] CategoryTenantKeyColumns =
            ["restaurant_id", "id"];

        private static readonly string[] PublicationTenantCategoryColumns =
            ["restaurant_id", "category_id"];

        private static readonly string[] PublicMenuIndexColumns =
            ["restaurant_id", "branch_id", "is_published", "display_order_override"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_menu_categories_restaurant_id_id",
                schema: "catalog",
                table: "menu_categories",
                columns: CategoryTenantKeyColumns);

            migrationBuilder.CreateTable(
                name: "branch_category_publications",
                schema: "catalog",
                columns: table => new
                {
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    restaurant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    display_order_override = table.Column<int>(type: "integer", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_category_publications", x => new { x.branch_id, x.category_id });
                    table.CheckConstraint("ck_branch_category_publications_display_order_override", "display_order_override IS NULL OR display_order_override >= 0");
                    table.ForeignKey(
                        name: "FK_branch_category_publications_menu_categories_restaurant_id_~",
                        columns: x => new { x.restaurant_id, x.category_id },
                        principalSchema: "catalog",
                        principalTable: "menu_categories",
                        principalColumns: CategoryTenantKeyColumns,
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_branch_category_publications_public_menu",
                schema: "catalog",
                table: "branch_category_publications",
                columns: PublicMenuIndexColumns);

            migrationBuilder.CreateIndex(
                name: "ix_branch_category_publications_restaurant_id_category_id",
                schema: "catalog",
                table: "branch_category_publications",
                columns: PublicationTenantCategoryColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_category_publications",
                schema: "catalog");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_menu_categories_restaurant_id_id",
                schema: "catalog",
                table: "menu_categories");
        }
    }
}
