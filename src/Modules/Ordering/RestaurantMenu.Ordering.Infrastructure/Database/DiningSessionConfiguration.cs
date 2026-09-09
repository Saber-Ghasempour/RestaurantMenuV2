using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantMenu.Ordering.Domain.DiningSessions;

namespace RestaurantMenu.Ordering.Infrastructure.Database;
internal sealed class DiningSessionConfiguration : IEntityTypeConfiguration<DiningSession>
{
    public void Configure(EntityTypeBuilder<DiningSession> builder)
    {
        builder.ToTable("dining_sessions", table => table.HasCheckConstraint(
            "ck_dining_sessions_expiry", "expires_at_utc > created_at_utc"));
        builder.HasKey(session => session.Id);
        builder.Property(session => session.Id).HasConversion(id => id.Value, value => new DiningSessionId(value)).HasColumnName("id").ValueGeneratedNever();
        builder.Property(session => session.TokenHash).HasColumnName("token_hash").HasMaxLength(DiningSession.MaxTokenHashLength).IsRequired();
        builder.Property(session => session.RestaurantId).HasColumnName("restaurant_id").IsRequired();
        builder.Property(session => session.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(session => session.DiningTableId).HasColumnName("dining_table_id").IsRequired();
        builder.Property(session => session.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(session => session.ExpiresAtUtc).HasColumnName("expires_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(session => session.RevokedAtUtc).HasColumnName("revoked_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(session => session.LastSeenAtUtc).HasColumnName("last_seen_at_utc").HasColumnType("timestamp with time zone");
        builder.HasIndex(session => session.TokenHash).IsUnique().HasDatabaseName("ux_dining_sessions_token_hash");
        builder.HasIndex(session => session.ExpiresAtUtc).HasDatabaseName("ix_dining_sessions_expires_at_utc");
        builder.HasIndex(session => new { session.RestaurantId, session.BranchId, session.DiningTableId })
            .HasDatabaseName("ix_dining_sessions_scope");
        builder.Ignore(session => session.DomainEvents);
    }
}
