using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantMenu.Feedback.Domain.FeedbackEntries;
namespace RestaurantMenu.Feedback.Infrastructure.Database;
internal sealed class FeedbackEntryConfiguration : IEntityTypeConfiguration<FeedbackEntry>
{
    public void Configure(EntityTypeBuilder<FeedbackEntry> b)
    {
        b.ToTable("feedback_entries", table => table.HasCheckConstraint("ck_feedback_rating", "rating between 1 and 5"));
        b.HasKey(x => x.Id); b.Property(x => x.Id).HasConversion(x => x.Value, x => new(x)).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.RestaurantId).HasColumnName("restaurant_id"); b.Property(x => x.BranchId).HasColumnName("branch_id");
        b.Property(x => x.OrderId).HasColumnName("order_id"); b.Property(x => x.OrderLineId).HasColumnName("order_line_id");
        b.Property(x => x.DiningSessionId).HasColumnName("dining_session_id"); b.Property(x => x.Rating).HasColumnName("rating");
        b.Property(x => x.Sentiment).HasConversion<string>().HasColumnName("sentiment").HasMaxLength(16);
        b.Property(x => x.Comment).HasColumnName("comment").HasMaxLength(FeedbackEntry.MaxCommentLength);
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
        b.Property(x => x.IsHidden).HasColumnName("is_hidden"); b.Property(x => x.HiddenAtUtc).HasColumnName("hidden_at_utc").HasColumnType("timestamp with time zone");
        b.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken(); b.Ignore(x => x.DomainEvents);
        b.HasIndex(x => new { x.DiningSessionId, x.OrderId, x.OrderLineId }).IsUnique().HasFilter("order_line_id is not null").HasDatabaseName("ux_feedback_session_order_line");
        b.HasIndex(x => new { x.DiningSessionId, x.OrderId }).IsUnique().HasFilter("order_line_id is null").HasDatabaseName("ux_feedback_session_order");
        b.HasIndex(x => new { x.RestaurantId, x.CreatedAtUtc }).HasDatabaseName("ix_feedback_restaurant_created");
        b.HasIndex(x => new { x.RestaurantId, x.BranchId, x.CreatedAtUtc }).HasDatabaseName("ix_feedback_branch_created");
    }
}
