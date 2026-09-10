using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace RestaurantMenu.Ordering.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionalMessaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "ordering",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    event_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    dead_lettered_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => new { x.message_id, x.consumer });
                    table.CheckConstraint("ck_inbox_attempt_count", "attempt_count >= 0");
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "ordering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    event_version = table.Column<int>(type: "integer", nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_version = table.Column<long>(type: "bigint", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    next_attempt_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    dead_lettered_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    lock_id = table.Column<Guid>(type: "uuid", nullable: true),
                    locked_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                    table.CheckConstraint("ck_outbox_aggregate_version", "aggregate_version > 0");
                    table.CheckConstraint("ck_outbox_attempt_count", "attempt_count >= 0");
                    table.CheckConstraint("ck_outbox_event_version", "event_version > 0");
                });

            migrationBuilder.CreateIndex(
                name: "ix_inbox_dead_lettered",
                schema: "ordering",
                table: "inbox_messages",
                column: "dead_lettered_at_utc",
                filter: "dead_lettered_at_utc IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_pending",
                schema: "ordering",
                table: "outbox_messages",
                columns: new[] { "next_attempt_at_utc", "occurred_at_utc", "id" },
                filter: "processed_at_utc IS NULL AND dead_lettered_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_outbox_aggregate_version",
                schema: "ordering",
                table: "outbox_messages",
                columns: new[] { "aggregate_id", "aggregate_version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "ordering");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "ordering");
        }
    }
}
