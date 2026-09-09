using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace RestaurantMenu.Ordering.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdersAndIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "orders",
                schema: "ordering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_number = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    restaurant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dining_table_id = table.Column<Guid>(type: "uuid", nullable: false),
                    table_display_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    dining_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    currency = table.Column<string>(type: "character(3)", nullable: false),
                    subtotal_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    customer_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    accepted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                    table.CheckConstraint("ck_orders_amounts", "subtotal_amount >= 0 AND total_amount >= 0");
                    table.CheckConstraint("ck_orders_currency", "char_length(currency) = 3");
                });

            migrationBuilder.CreateTable(
                name: "idempotency_records",
                schema: "ordering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    request_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    response_status = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_records", x => x.id);
                    table.CheckConstraint("ck_idempotency_records_expiry", "expires_at_utc > created_at_utc");
                    table.ForeignKey(
                        name: "FK_idempotency_records_orders_resource_id",
                        column: x => x.resource_id,
                        principalSchema: "ordering",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_lines",
                schema: "ordering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    variant_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    unit_price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character(3)", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    line_total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_lines", x => x.id);
                    table.CheckConstraint("ck_order_lines_amounts", "unit_price_amount >= 0 AND line_total_amount = unit_price_amount * quantity");
                    table.CheckConstraint("ck_order_lines_currency", "char_length(currency) = 3");
                    table.CheckConstraint("ck_order_lines_quantity", "quantity BETWEEN 1 AND 99");
                    table.ForeignKey(
                        name: "FK_order_lines_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "ordering",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_records_expires_at_utc",
                schema: "ordering",
                table: "idempotency_records",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_records_resource_id",
                schema: "ordering",
                table: "idempotency_records",
                column: "resource_id");

            migrationBuilder.CreateIndex(
                name: "ux_idempotency_records_scope_key",
                schema: "ordering",
                table: "idempotency_records",
                columns: new[] { "scope", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_order_lines_order_id",
                schema: "ordering",
                table: "order_lines",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_branch_queue",
                schema: "ordering",
                table: "orders",
                columns: new[] { "restaurant_id", "branch_id", "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_orders_dining_session",
                schema: "ordering",
                table: "orders",
                columns: new[] { "dining_session_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_orders_public_number",
                schema: "ordering",
                table: "orders",
                column: "public_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotency_records",
                schema: "ordering");

            migrationBuilder.DropTable(
                name: "order_lines",
                schema: "ordering");

            migrationBuilder.DropTable(
                name: "orders",
                schema: "ordering");
        }
    }
}
