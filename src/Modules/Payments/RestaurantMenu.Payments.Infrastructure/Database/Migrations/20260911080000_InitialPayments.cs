using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace RestaurantMenu.Payments.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "payments");

            migrationBuilder.CreateTable(
                name: "payments",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    restaurant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dining_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    connected_account_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    subtotal_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    discount_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    tax_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    tip_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    other_fee_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    gross_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character(3)", nullable: false),
                    commission_rate_basis_points = table.Column<int>(type: "integer", nullable: false),
                    platform_fee_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    restaurant_proceeds_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    provider_payment_intent_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    refunded_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    refunded_platform_fee_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    succeeded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.id);
                    table.CheckConstraint("ck_payments_commission", "commission_rate_basis_points BETWEEN 1 AND 10000 AND platform_fee_amount_minor >= 0 AND platform_fee_amount_minor <= gross_amount_minor AND restaurant_proceeds_amount_minor = gross_amount_minor - platform_fee_amount_minor");
                    table.CheckConstraint("ck_payments_components", "subtotal_amount_minor >= 0 AND discount_amount_minor >= 0 AND discount_amount_minor <= subtotal_amount_minor AND tax_amount_minor >= 0 AND tip_amount_minor >= 0 AND other_fee_amount_minor >= 0");
                    table.CheckConstraint("ck_payments_currency", "currency ~ '^[A-Z]{3}$'");
                    table.CheckConstraint("ck_payments_gross", "gross_amount_minor = subtotal_amount_minor - discount_amount_minor + tax_amount_minor + tip_amount_minor + other_fee_amount_minor AND gross_amount_minor > 0");
                    table.CheckConstraint("ck_payments_refunds", "refunded_amount_minor BETWEEN 0 AND gross_amount_minor AND refunded_platform_fee_amount_minor BETWEEN 0 AND platform_fee_amount_minor");
                    table.CheckConstraint("ck_payments_status", "status IN ('Pending', 'Processing', 'Succeeded', 'PartiallyRefunded', 'Refunded', 'Failed', 'Cancelled')");
                });

            migrationBuilder.CreateTable(
                name: "restaurant_payment_profiles",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    restaurant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stripe_account_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    country = table.Column<string>(type: "character(2)", nullable: false),
                    currency = table.Column<string>(type: "character(3)", nullable: false),
                    commission_rate_basis_points = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_restaurant_payment_profiles", x => x.id);
                    table.CheckConstraint("ck_payment_profiles_commission", "commission_rate_basis_points BETWEEN 1 AND 10000");
                    table.CheckConstraint("ck_payment_profiles_country", "country ~ '^[A-Z]{2}$'");
                    table.CheckConstraint("ck_payment_profiles_currency", "currency ~ '^[A-Z]{3}$'");
                });

            migrationBuilder.CreateTable(
                name: "payment_events",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    provider_event_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    amount_minor = table.Column<long>(type: "bigint", nullable: true),
                    platform_fee_amount_minor = table.Column<long>(type: "bigint", nullable: true),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_events", x => x.id);
                    table.CheckConstraint("ck_payment_events_amount", "amount_minor IS NULL OR amount_minor >= 0");
                    table.CheckConstraint("ck_payment_events_fee", "platform_fee_amount_minor IS NULL OR platform_fee_amount_minor >= 0");
                    table.ForeignKey(
                        name: "FK_payment_events_payments_payment_id",
                        column: x => x.payment_id,
                        principalSchema: "payments",
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payment_events_payment_occurred",
                schema: "payments",
                table: "payment_events",
                columns: new[] { "payment_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_payment_events_provider_event",
                schema: "payments",
                table: "payment_events",
                column: "provider_event_id",
                unique: true,
                filter: "provider_event_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payments_restaurant_created",
                schema: "payments",
                table: "payments",
                columns: new[] { "restaurant_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_payments_order",
                schema: "payments",
                table: "payments",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_payments_provider_intent",
                schema: "payments",
                table: "payments",
                column: "provider_payment_intent_id",
                unique: true,
                filter: "provider_payment_intent_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_payment_profiles_restaurant",
                schema: "payments",
                table: "restaurant_payment_profiles",
                column: "restaurant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_payment_profiles_stripe_account",
                schema: "payments",
                table: "restaurant_payment_profiles",
                column: "stripe_account_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_events",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "restaurant_payment_profiles",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "payments",
                schema: "payments");
        }
    }
}
