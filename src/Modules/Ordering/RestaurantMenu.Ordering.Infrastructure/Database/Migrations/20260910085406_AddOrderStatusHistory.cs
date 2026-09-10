using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace RestaurantMenu.Ordering.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderStatusHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "order_status_history",
                schema: "ordering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    to_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    changed_by_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    changed_by_subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_status_history", x => x.id);
                    table.CheckConstraint("ck_order_status_history_actor", "changed_by_type IN ('Guest', 'Staff', 'System')");
                    table.CheckConstraint("ck_order_status_history_from", "from_status IS NULL OR from_status IN ('Placed', 'Accepted', 'Preparing', 'Ready', 'Served', 'Completed', 'Rejected', 'Cancelled')");
                    table.CheckConstraint("ck_order_status_history_to", "to_status IN ('Placed', 'Accepted', 'Preparing', 'Ready', 'Served', 'Completed', 'Rejected', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_order_status_history_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "ordering",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_status",
                schema: "ordering",
                table: "orders",
                sql: "status IN ('Placed', 'Accepted', 'Preparing', 'Ready', 'Served', 'Completed', 'Rejected', 'Cancelled')");

            migrationBuilder.Sql(
                """
                INSERT INTO ordering.order_status_history
                    (id, order_id, from_status, to_status, changed_by_type,
                     changed_by_subject, reason, created_at_utc)
                SELECT gen_random_uuid(), id, NULL, status,
                       CASE WHEN status = 'Placed' THEN 'Guest' ELSE 'System' END,
                       NULL, 'Backfilled when status history was introduced', created_at_utc
                FROM ordering.orders;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_order_status_history_order_created",
                schema: "ordering",
                table: "order_status_history",
                columns: new[] { "order_id", "created_at_utc", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_status_history",
                schema: "ordering");

            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_status",
                schema: "ordering",
                table: "orders");
        }
    }
}
