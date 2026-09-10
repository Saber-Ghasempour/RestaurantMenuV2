using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantMenu.Ordering.Domain.Orders;

namespace RestaurantMenu.Ordering.Infrastructure.Database;

internal sealed class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("order_status_history", table =>
        {
            table.HasCheckConstraint("ck_order_status_history_from", "from_status IS NULL OR from_status IN ('Placed', 'Accepted', 'Preparing', 'Ready', 'Served', 'Completed', 'Rejected', 'Cancelled')");
            table.HasCheckConstraint("ck_order_status_history_to", "to_status IN ('Placed', 'Accepted', 'Preparing', 'Ready', 'Served', 'Completed', 'Rejected', 'Cancelled')");
            table.HasCheckConstraint("ck_order_status_history_actor", "changed_by_type IN ('Guest', 'Staff', 'System')");
        });
        builder.HasKey(value => value.Id);
        builder.Property(value => value.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(value => value.OrderId).HasConversion(id => id.Value, value => new OrderId(value)).HasColumnName("order_id").IsRequired();
        builder.Property(value => value.FromStatus).HasConversion<string>().HasColumnName("from_status").HasMaxLength(32);
        builder.Property(value => value.ToStatus).HasConversion<string>().HasColumnName("to_status").HasMaxLength(32).IsRequired();
        builder.Property(value => value.ChangedByType).HasConversion<string>().HasColumnName("changed_by_type").HasMaxLength(32).IsRequired();
        builder.Property(value => value.ChangedBySubject).HasColumnName("changed_by_subject").HasMaxLength(OrderStatusHistory.MaxSubjectLength);
        builder.Property(value => value.Reason).HasColumnName("reason").HasMaxLength(OrderStatusHistory.MaxReasonLength);
        builder.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(value => new { value.OrderId, value.CreatedAtUtc, value.Id })
            .HasDatabaseName("ix_order_status_history_order_created");
    }
}
