using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantMenu.Ordering.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialDiningSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ordering");

            migrationBuilder.CreateTable(
                name: "dining_sessions",
                schema: "ordering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    restaurant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dining_table_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_seen_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dining_sessions", x => x.id);
                    table.CheckConstraint("ck_dining_sessions_expiry", "expires_at_utc > created_at_utc");
                });

            migrationBuilder.CreateIndex(
                name: "ix_dining_sessions_expires_at_utc",
                schema: "ordering",
                table: "dining_sessions",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_dining_sessions_scope",
                schema: "ordering",
                table: "dining_sessions",
                columns: ["restaurant_id", "branch_id", "dining_table_id"]);

            migrationBuilder.CreateIndex(
                name: "ux_dining_sessions_token_hash",
                schema: "ordering",
                table: "dining_sessions",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dining_sessions",
                schema: "ordering");
        }
    }
}
