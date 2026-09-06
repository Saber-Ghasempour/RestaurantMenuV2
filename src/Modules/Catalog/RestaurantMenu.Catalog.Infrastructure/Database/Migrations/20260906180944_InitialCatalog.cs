using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantMenu.Catalog.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        private static readonly string[] RestaurantDisplayOrderColumns =
            ["restaurant_id", "display_order"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.CreateTable(
                name: "menu_categories",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    restaurant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_categories", x => x.id);
                    table.ForeignKey(
                        name: "FK_menu_categories_menu_categories_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "catalog",
                        principalTable: "menu_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_menu_categories_parent_id",
                schema: "catalog",
                table: "menu_categories",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_menu_categories_restaurant_id_display_order",
                schema: "catalog",
                table: "menu_categories",
                columns: RestaurantDisplayOrderColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "menu_categories",
                schema: "catalog");
        }
    }
}
