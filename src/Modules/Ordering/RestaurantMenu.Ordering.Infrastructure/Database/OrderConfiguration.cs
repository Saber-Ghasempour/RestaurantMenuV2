using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantMenu.Ordering.Domain.Orders;

namespace RestaurantMenu.Ordering.Infrastructure.Database;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", table =>
        {
            table.HasCheckConstraint("ck_orders_amounts",
                "subtotal_amount >= 0 AND tax_amount >= 0 AND total_amount = subtotal_amount + tax_amount");
            table.HasCheckConstraint("ck_orders_currency", "char_length(currency) = 3");
            table.HasCheckConstraint("ck_orders_status", "status IN ('Placed', 'Accepted', 'Preparing', 'Ready', 'Served', 'Completed', 'Rejected', 'Cancelled')");
        });
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).HasConversion(id => id.Value, value => new OrderId(value)).HasColumnName("id").ValueGeneratedNever();
        builder.Property(order => order.PublicNumber).HasColumnName("public_number").HasMaxLength(Order.MaxPublicNumberLength).IsRequired();
        builder.Property(order => order.RestaurantId).HasColumnName("restaurant_id").IsRequired();
        builder.Property(order => order.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(order => order.DiningTableId).HasColumnName("dining_table_id").IsRequired();
        builder.Property(order => order.TableDisplayName).HasColumnName("table_display_name").HasMaxLength(Order.MaxTableDisplayNameLength).IsRequired();
        builder.Property(order => order.DiningSessionId).HasColumnName("dining_session_id").IsRequired();
        builder.Property(order => order.Status).HasConversion<string>().HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(order => order.Currency).HasColumnName("currency").HasColumnType("character(3)").IsRequired();
        builder.Property(order => order.SubtotalAmount).HasColumnName("subtotal_amount").HasPrecision(18, 2).IsRequired();
        builder.Property(order => order.TaxAmount).HasColumnName("tax_amount").HasPrecision(18, 2).IsRequired();
        builder.Property(order => order.TotalAmount).HasColumnName("total_amount").HasPrecision(18, 2).IsRequired();
        builder.Property(order => order.CustomerNote).HasColumnName("customer_note").HasMaxLength(Order.MaxCustomerNoteLength);
        builder.Property(order => order.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(order => order.AcceptedAtUtc).HasColumnName("accepted_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(order => order.CompletedAtUtc).HasColumnName("completed_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(order => order.CancelledAtUtc).HasColumnName("cancelled_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(order => order.Version).HasColumnName("version").HasDefaultValue(1L).IsConcurrencyToken().IsRequired();
        builder.HasIndex(order => order.PublicNumber).IsUnique().HasDatabaseName("ux_orders_public_number");
        builder.HasIndex(order => new { order.RestaurantId, order.BranchId, order.Status, order.CreatedAtUtc }).HasDatabaseName("ix_orders_branch_queue");
        builder.HasIndex(order => new { order.DiningSessionId, order.CreatedAtUtc }).HasDatabaseName("ix_orders_dining_session");
        builder.HasMany(order => order.Lines).WithOne().HasForeignKey(line => line.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(order => order.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(order => order.StatusHistory).WithOne().HasForeignKey(value => value.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(order => order.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(order => order.DomainEvents);
    }
}
