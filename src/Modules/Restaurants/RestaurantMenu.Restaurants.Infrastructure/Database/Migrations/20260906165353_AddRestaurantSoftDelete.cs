using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantMenu.Restaurants.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at_utc",
                schema: "restaurants",
                table: "restaurants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "restaurants",
                table: "restaurants",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deleted_at_utc",
                schema: "restaurants",
                table: "restaurants");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "restaurants",
                table: "restaurants");
        }
    }
}
