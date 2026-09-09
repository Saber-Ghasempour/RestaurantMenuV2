using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantMenu.Restaurants.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "default_currency",
                schema: "restaurants",
                table: "restaurants",
                type: "character(3)",
                nullable: false,
                defaultValue: "USD");

            migrationBuilder.AddColumn<string>(
                name: "default_locale",
                schema: "restaurants",
                table: "restaurants",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "en-US");

            migrationBuilder.AddColumn<string>(
                name: "time_zone_id",
                schema: "restaurants",
                table: "restaurants",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Etc/UTC");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "default_currency",
                schema: "restaurants",
                table: "restaurants");

            migrationBuilder.DropColumn(
                name: "default_locale",
                schema: "restaurants",
                table: "restaurants");

            migrationBuilder.DropColumn(
                name: "time_zone_id",
                schema: "restaurants",
                table: "restaurants");
        }
    }
}
