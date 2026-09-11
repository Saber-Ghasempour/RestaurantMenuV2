using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantMenu.Ordering.Domain.Orders;

namespace RestaurantMenu.Ordering.Infrastructure.Database;

internal sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("order_lines", table =>
        {
            table.HasCheckConstraint("ck_order_lines_quantity", $"quantity BETWEEN 1 AND {OrderLine.MaxQuantity}");
            table.HasCheckConstraint("ck_order_lines_amounts",
                "unit_price_amount >= 0 AND net_amount >= 0 AND tax_amount >= 0 AND line_total_amount = net_amount + tax_amount");
            table.HasCheckConstraint("ck_order_lines_currency", "char_length(currency) = 3");
            table.HasCheckConstraint("ck_order_lines_tax_rate", "tax_rate_basis_points BETWEEN 0 AND 10000");
            table.HasCheckConstraint("ck_order_lines_tax_behavior", "tax_behavior IN ('Inclusive', 'Exclusive')");
        });
        builder.HasKey(line => line.Id);
        builder.Property(line => line.Id).HasConversion(id => id.Value, value => new OrderLineId(value)).HasColumnName("id").ValueGeneratedNever();
        builder.Property(line => line.OrderId).HasConversion(id => id.Value, value => new OrderId(value)).HasColumnName("order_id").IsRequired();
        builder.Property(line => line.MenuItemId).HasColumnName("menu_item_id");
        builder.Property(line => line.VariantId).HasColumnName("variant_id");
        builder.Property(line => line.ItemName).HasColumnName("item_name").HasMaxLength(OrderLine.MaxItemNameLength).IsRequired();
        builder.Property(line => line.VariantName).HasColumnName("variant_name").HasMaxLength(OrderLine.MaxVariantNameLength);
        builder.Property(line => line.UnitPriceAmount).HasColumnName("unit_price_amount").HasPrecision(18, 2).IsRequired();
        builder.Property(line => line.Currency).HasColumnName("currency").HasColumnType("character(3)").IsRequired();
        builder.Property(line => line.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(line => line.NetAmount).HasColumnName("net_amount").HasPrecision(18, 2).IsRequired();
        builder.Property(line => line.TaxAmount).HasColumnName("tax_amount").HasPrecision(18, 2).IsRequired();
        builder.Property(line => line.LineTotalAmount).HasColumnName("line_total_amount").HasPrecision(18, 2).IsRequired();
        builder.Property(line => line.TaxRateBasisPoints).HasColumnName("tax_rate_basis_points").IsRequired();
        builder.Property(line => line.TaxBehavior).HasConversion<string>().HasColumnName("tax_behavior")
            .HasMaxLength(16).IsRequired();
        builder.Property(line => line.Note).HasColumnName("note").HasMaxLength(OrderLine.MaxNoteLength);
        builder.HasIndex(line => line.OrderId).HasDatabaseName("ix_order_lines_order_id");
    }
}
