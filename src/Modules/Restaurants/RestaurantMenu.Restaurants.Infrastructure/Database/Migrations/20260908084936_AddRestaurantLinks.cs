using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantMenu.Restaurants.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "facebook_url",
                schema: "restaurants",
                table: "restaurants",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "instagram_url",
                schema: "restaurants",
                table: "restaurants",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "telegram_url",
                schema: "restaurants",
                table: "restaurants",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "twitter_url",
                schema: "restaurants",
                table: "restaurants",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "website_url",
                schema: "restaurants",
                table: "restaurants",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "whats_app_url",
                schema: "restaurants",
                table: "restaurants",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "facebook_url",
                schema: "restaurants",
                table: "restaurants");

            migrationBuilder.DropColumn(
                name: "instagram_url",
                schema: "restaurants",
                table: "restaurants");

            migrationBuilder.DropColumn(
                name: "telegram_url",
                schema: "restaurants",
                table: "restaurants");

            migrationBuilder.DropColumn(
                name: "twitter_url",
                schema: "restaurants",
                table: "restaurants");

            migrationBuilder.DropColumn(
                name: "website_url",
                schema: "restaurants",
                table: "restaurants");

            migrationBuilder.DropColumn(
                name: "whats_app_url",
                schema: "restaurants",
                table: "restaurants");
        }
    }
}
