using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace RestaurantMenu.Restaurants.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchMemberships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "branch_memberships",
                schema: "restaurants",
                columns: table => new
                {
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    restaurant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_memberships", x => new { x.branch_id, x.subject });
                    table.CheckConstraint("ck_branch_memberships_role", "role IN ('Manager', 'Cashier', 'Kitchen', 'Waiter')");
                    table.CheckConstraint("ck_branch_memberships_status", "status IN ('Active', 'Suspended')");
                    table.ForeignKey(
                        name: "FK_branch_memberships_branches_restaurant_id_branch_id",
                        columns: x => new { x.restaurant_id, x.branch_id },
                        principalSchema: "restaurants",
                        principalTable: "branches",
                        principalColumns: new[] { "restaurant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_branch_memberships_restaurant_memberships_restaurant_id_sub~",
                        columns: x => new { x.restaurant_id, x.subject },
                        principalSchema: "restaurants",
                        principalTable: "restaurant_memberships",
                        principalColumns: new[] { "restaurant_id", "subject" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_branch_memberships_restaurant_id_branch_id",
                schema: "restaurants",
                table: "branch_memberships",
                columns: new[] { "restaurant_id", "branch_id" });

            migrationBuilder.CreateIndex(
                name: "ix_branch_memberships_restaurant_subject_status",
                schema: "restaurants",
                table: "branch_memberships",
                columns: new[] { "restaurant_id", "subject", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_memberships",
                schema: "restaurants");
        }
    }
}
