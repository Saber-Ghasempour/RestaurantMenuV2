using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantMenu.Ordering.Infrastructure.Messaging;

namespace RestaurantMenu.Ordering.Infrastructure.Database;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages", table =>
        {
            table.HasCheckConstraint("ck_outbox_event_version", "event_version > 0");
            table.HasCheckConstraint("ck_outbox_aggregate_version", "aggregate_version > 0");
            table.HasCheckConstraint("ck_outbox_attempt_count", "attempt_count >= 0");
        });
        builder.HasKey(value => value.Id);
        builder.Property(value => value.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(value => value.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(value => value.EventVersion).HasColumnName("event_version").IsRequired();
        builder.Property(value => value.AggregateId).HasColumnName("aggregate_id").IsRequired();
        builder.Property(value => value.AggregateVersion).HasColumnName("aggregate_version").IsRequired();
        builder.Property(value => value.OccurredAtUtc).HasColumnName("occurred_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(value => value.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(value => value.AttemptCount).HasColumnName("attempt_count").HasDefaultValue(0).IsRequired();
        builder.Property(value => value.NextAttemptAtUtc).HasColumnName("next_attempt_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(value => value.ProcessedAtUtc).HasColumnName("processed_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(value => value.DeadLetteredAtUtc).HasColumnName("dead_lettered_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(value => value.LastError).HasColumnName("last_error").HasMaxLength(2000);
        builder.Property(value => value.LockId).HasColumnName("lock_id");
        builder.Property(value => value.LockedUntilUtc).HasColumnName("locked_until_utc").HasColumnType("timestamp with time zone");
        builder.HasIndex(value => new { value.NextAttemptAtUtc, value.OccurredAtUtc, value.Id })
            .HasFilter("processed_at_utc IS NULL AND dead_lettered_at_utc IS NULL")
            .HasDatabaseName("ix_outbox_pending");
        builder.HasIndex(value => new { value.AggregateId, value.AggregateVersion })
            .IsUnique().HasDatabaseName("ux_outbox_aggregate_version");
    }
}
