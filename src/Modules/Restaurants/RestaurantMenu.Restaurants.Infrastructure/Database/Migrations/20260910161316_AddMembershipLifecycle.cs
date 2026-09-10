using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace RestaurantMenu.Restaurants.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "restaurants",
                table: "restaurant_memberships",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<long>(
                name: "version",
                schema: "restaurants",
                table: "restaurant_memberships",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.CreateTable(
                name: "membership_invitations",
                schema: "restaurants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    restaurant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    invited_by_subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    accepted_by_subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    accepted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_membership_invitations", x => x.id);
                    table.ForeignKey(
                        name: "FK_membership_invitations_restaurants_restaurant_id",
                        column: x => x.restaurant_id,
                        principalSchema: "restaurants",
                        principalTable: "restaurants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_membership_invitations_active_email",
                schema: "restaurants",
                table: "membership_invitations",
                columns: new[] { "restaurant_id", "email" },
                unique: true,
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "ux_membership_invitations_token_hash",
                schema: "restaurants",
                table: "membership_invitations",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "membership_invitations",
                schema: "restaurants");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "restaurants",
                table: "restaurant_memberships");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "restaurants",
                table: "restaurant_memberships");
        }
    }
}