using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantMenu.Ordering.Infrastructure.Messaging;

namespace RestaurantMenu.Ordering.Infrastructure.Database;

internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages", table =>
            table.HasCheckConstraint("ck_inbox_attempt_count", "attempt_count >= 0"));
        builder.HasKey(value => new { value.MessageId, value.Consumer });
        builder.Property(value => value.MessageId).HasColumnName("message_id").ValueGeneratedNever();
        builder.Property(value => value.Consumer).HasColumnName("consumer").HasMaxLength(200);
        builder.Property(value => value.EventName).HasColumnName("event_name").HasMaxLength(200);
        builder.Property(value => value.ReceivedAtUtc).HasColumnName("received_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(value => value.AttemptCount).HasColumnName("attempt_count").HasDefaultValue(0);
        builder.Property(value => value.ProcessedAtUtc).HasColumnName("processed_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(value => value.DeadLetteredAtUtc).HasColumnName("dead_lettered_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(value => value.LastError).HasColumnName("last_error").HasMaxLength(2000);
        builder.HasIndex(value => value.DeadLetteredAtUtc).HasFilter("dead_lettered_at_utc IS NOT NULL")
            .HasDatabaseName("ix_inbox_dead_lettered");
    }
}
