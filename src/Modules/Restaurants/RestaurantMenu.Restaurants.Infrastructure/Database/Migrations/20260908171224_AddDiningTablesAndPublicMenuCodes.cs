using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861 // EF Core generates inline column-name arrays.

namespace RestaurantMenu.Restaurants.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDiningTablesAndPublicMenuCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_branches_restaurant_id_id",
                schema: "restaurants",
                table: "branches",
                columns: new[] { "restaurant_id", "id" });

            migrationBuilder.CreateTable(
                name: "dining_tables",
                schema: "restaurants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    restaurant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    display_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    capacity = table.Column<short>(type: "smallint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dining_tables", x => x.id);
                    table.UniqueConstraint("AK_dining_tables_restaurant_id_branch_id_id", x => new { x.restaurant_id, x.branch_id, x.id });
                    table.CheckConstraint("ck_dining_tables_capacity_positive", "capacity IS NULL OR capacity > 0");
                    table.CheckConstraint("ck_dining_tables_number_positive", "number > 0");
                    table.ForeignKey(
                        name: "FK_dining_tables_branches_restaurant_id_branch_id",
                        columns: x => new { x.restaurant_id, x.branch_id },
                        principalSchema: "restaurants",
                        principalTable: "branches",
                        principalColumns: new[] { "restaurant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "public_menu_codes",
                schema: "restaurants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    restaurant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    dining_table_id = table.Column<Guid>(type: "uuid", nullable: true),
                    purpose = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    rotated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_public_menu_codes", x => x.id);
                    table.CheckConstraint("ck_public_menu_codes_purpose_scope", "(purpose = 'MenuOnly' AND dining_table_id IS NULL) OR (purpose = 'DineInOrdering' AND branch_id IS NOT NULL AND dining_table_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_public_menu_codes_branches_restaurant_id_branch_id",
                        columns: x => new { x.restaurant_id, x.branch_id },
                        principalSchema: "restaurants",
                        principalTable: "branches",
                        principalColumns: new[] { "restaurant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_public_menu_codes_dining_tables_restaurant_id_branch_id_din~",
                        columns: x => new { x.restaurant_id, x.branch_id, x.dining_table_id },
                        principalSchema: "restaurants",
                        principalTable: "dining_tables",
                        principalColumns: new[] { "restaurant_id", "branch_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_public_menu_codes_restaurants_restaurant_id",
                        column: x => x.restaurant_id,
                        principalSchema: "restaurants",
                        principalTable: "restaurants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dining_tables_restaurant_branch_number_id",
                schema: "restaurants",
                table: "dining_tables",
                columns: new[] { "restaurant_id", "branch_id", "number", "id" });

            migrationBuilder.CreateIndex(
                name: "ux_dining_tables_branch_id_number",
                schema: "restaurants",
                table: "dining_tables",
                columns: new[] { "branch_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_public_menu_codes_expires_at_utc",
                schema: "restaurants",
                table: "public_menu_codes",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_public_menu_codes_restaurant_created_id",
                schema: "restaurants",
                table: "public_menu_codes",
                columns: new[] { "restaurant_id", "created_at_utc", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_public_menu_codes_restaurant_id_branch_id_dining_table_id",
                schema: "restaurants",
                table: "public_menu_codes",
                columns: new[] { "restaurant_id", "branch_id", "dining_table_id" });

            migrationBuilder.CreateIndex(
                name: "ux_public_menu_codes_code_hash",
                schema: "restaurants",
                table: "public_menu_codes",
                column: "code_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "public_menu_codes",
                schema: "restaurants");

            migrationBuilder.DropTable(
                name: "dining_tables",
                schema: "restaurants");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_branches_restaurant_id_id",
                schema: "restaurants",
                table: "branches");
        }
    }
}
