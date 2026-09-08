using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantMenu.Restaurants.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantTextProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "about",
                schema: "restaurants",
                table: "restaurants",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address",
                schema: "restaurants",
                table: "restaurants",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "restaurants",
                table: "restaurants",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "about",
                schema: "restaurants",
                table: "restaurants");

            migrationBuilder.DropColumn(
                name: "address",
                schema: "restaurants",
                table: "restaurants");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "restaurants",
                table: "restaurants");
        }
    }
}
