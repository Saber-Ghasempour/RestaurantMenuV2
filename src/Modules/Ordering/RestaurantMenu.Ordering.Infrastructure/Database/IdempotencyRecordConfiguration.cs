using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantMenu.Ordering.Domain.Orders;

namespace RestaurantMenu.Ordering.Infrastructure.Database;

internal sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records", table => table.HasCheckConstraint(
            "ck_idempotency_records_expiry", "expires_at_utc > created_at_utc"));
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(record => record.Scope).HasColumnName("scope").HasMaxLength(IdempotencyRecord.MaxScopeLength).IsRequired();
        builder.Property(record => record.Key).HasColumnName("idempotency_key").HasMaxLength(IdempotencyRecord.MaxKeyLength).IsRequired();
        builder.Property(record => record.RequestHash).HasColumnName("request_hash").HasMaxLength(IdempotencyRecord.RequestHashLength).IsFixedLength().IsRequired();
        builder.Property(record => record.ResourceId).HasConversion(id => id.Value, value => new OrderId(value)).HasColumnName("resource_id").IsRequired();
        builder.Property(record => record.ResponseStatus).HasColumnName("response_status").IsRequired();
        builder.Property(record => record.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(record => record.ExpiresAtUtc).HasColumnName("expires_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(record => new { record.Scope, record.Key }).IsUnique().HasDatabaseName("ux_idempotency_records_scope_key");
        builder.HasIndex(record => record.ExpiresAtUtc).HasDatabaseName("ix_idempotency_records_expires_at_utc");
        builder.HasOne<Order>().WithMany().HasForeignKey(record => record.ResourceId).OnDelete(DeleteBehavior.Restrict);
    }
}
