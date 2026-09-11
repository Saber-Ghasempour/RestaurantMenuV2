using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantMenu.Ordering.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderTaxSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_amounts",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_order_lines_amounts",
                schema: "ordering",
                table: "order_lines");

            migrationBuilder.AddColumn<decimal>(
                name: "tax_amount",
                schema: "ordering",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "net_amount",
                schema: "ordering",
                table: "order_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_amount",
                schema: "ordering",
                table: "order_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "tax_behavior",
                schema: "ordering",
                table: "order_lines",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Exclusive");

            migrationBuilder.AddColumn<int>(
                name: "tax_rate_basis_points",
                schema: "ordering",
                table: "order_lines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                "UPDATE ordering.order_lines SET net_amount = line_total_amount;");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_amounts",
                schema: "ordering",
                table: "orders",
                sql: "subtotal_amount >= 0 AND tax_amount >= 0 AND total_amount = subtotal_amount + tax_amount");

            migrationBuilder.AddCheckConstraint(
                name: "ck_order_lines_amounts",
                schema: "ordering",
                table: "order_lines",
                sql: "unit_price_amount >= 0 AND net_amount >= 0 AND tax_amount >= 0 AND line_total_amount = net_amount + tax_amount");

            migrationBuilder.AddCheckConstraint(
                name: "ck_order_lines_tax_behavior",
                schema: "ordering",
                table: "order_lines",
                sql: "tax_behavior IN ('Inclusive', 'Exclusive')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_order_lines_tax_rate",
                schema: "ordering",
                table: "order_lines",
                sql: "tax_rate_basis_points BETWEEN 0 AND 10000");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_amounts",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_order_lines_amounts",
                schema: "ordering",
                table: "order_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_order_lines_tax_behavior",
                schema: "ordering",
                table: "order_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_order_lines_tax_rate",
                schema: "ordering",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "tax_amount",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "net_amount",
                schema: "ordering",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "tax_amount",
                schema: "ordering",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "tax_behavior",
                schema: "ordering",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "tax_rate_basis_points",
                schema: "ordering",
                table: "order_lines");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_amounts",
                schema: "ordering",
                table: "orders",
                sql: "subtotal_amount >= 0 AND total_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_order_lines_amounts",
                schema: "ordering",
                table: "order_lines",
                sql: "unit_price_amount >= 0 AND line_total_amount = unit_price_amount * quantity");
        }
    }
}
