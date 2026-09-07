using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantMenu.Restaurants.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantMemberships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "restaurant_memberships",
                schema: "restaurants",
                columns: table => new
                {
                    restaurant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_restaurant_memberships", x => new { x.restaurant_id, x.subject });
                    table.ForeignKey(
                        name: "FK_restaurant_memberships_restaurants_restaurant_id",
                        column: x => x.restaurant_id,
                        principalSchema: "restaurants",
                        principalTable: "restaurants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_restaurant_memberships_subject",
                schema: "restaurants",
                table: "restaurant_memberships",
                column: "subject");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "restaurant_memberships",
                schema: "restaurants");
        }
    }
}
