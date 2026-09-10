using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace RestaurantMenu.Restaurants.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "membership_audit_entries",
                schema: "restaurants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    restaurant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    resource_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    resource_id = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    changes = table.Column<string>(type: "jsonb", nullable: true),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_membership_audit_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_membership_audit_entries_restaurants_restaurant_id",
                        column: x => x.restaurant_id,
                        principalSchema: "restaurants",
                        principalTable: "restaurants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_membership_audit_restaurant_occurred",
                schema: "restaurants",
                table: "membership_audit_entries",
                columns: new[] { "restaurant_id", "occurred_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "membership_audit_entries",
                schema: "restaurants");
        }
    }
}