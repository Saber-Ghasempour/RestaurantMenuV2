using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace RestaurantMenu.Feedback.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "feedback");

            migrationBuilder.CreateTable(
                name: "feedback_entries",
                schema: "feedback",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    restaurant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    dining_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<short>(type: "smallint", nullable: false),
                    sentiment = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false),
                    hidden_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feedback_entries", x => x.id);
                    table.CheckConstraint("ck_feedback_rating", "rating between 1 and 5");
                });

            migrationBuilder.CreateIndex(
                name: "ix_feedback_branch_created",
                schema: "feedback",
                table: "feedback_entries",
                columns: new[] { "restaurant_id", "branch_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_feedback_restaurant_created",
                schema: "feedback",
                table: "feedback_entries",
                columns: new[] { "restaurant_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_feedback_session_order",
                schema: "feedback",
                table: "feedback_entries",
                columns: new[] { "dining_session_id", "order_id" },
                unique: true,
                filter: "order_line_id is null");

            migrationBuilder.CreateIndex(
                name: "ux_feedback_session_order_line",
                schema: "feedback",
                table: "feedback_entries",
                columns: new[] { "dining_session_id", "order_id", "order_line_id" },
                unique: true,
                filter: "order_line_id is not null");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "feedback_entries",
                schema: "feedback");
        }
    }
}
