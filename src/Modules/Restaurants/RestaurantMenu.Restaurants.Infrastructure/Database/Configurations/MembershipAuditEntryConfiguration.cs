using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantMenu.Restaurants.Domain.Memberships;
using RestaurantMenu.Restaurants.Domain.Restaurants;
namespace RestaurantMenu.Restaurants.Infrastructure.Database.Configurations;

internal sealed class MembershipAuditEntryConfiguration : IEntityTypeConfiguration<MembershipAuditEntry>
{ public void Configure(EntityTypeBuilder<MembershipAuditEntry> b) { b.ToTable("membership_audit_entries"); b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever(); b.Property(x => x.RestaurantId).HasConversion(x => x.Value, x => new(x)).HasColumnName("restaurant_id"); b.Property(x => x.ActorSubject).HasColumnName("actor_subject").HasMaxLength(255); b.Property(x => x.Action).HasColumnName("action").HasMaxLength(64); b.Property(x => x.ResourceType).HasColumnName("resource_type").HasMaxLength(64); b.Property(x => x.ResourceId).HasColumnName("resource_id").HasMaxLength(320); b.Property(x => x.Changes).HasColumnName("changes").HasColumnType("jsonb"); b.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc").HasColumnType("timestamp with time zone"); b.HasIndex(x => new { x.RestaurantId, x.OccurredAtUtc }).HasDatabaseName("ix_membership_audit_restaurant_occurred"); b.HasOne<Restaurant>().WithMany().HasForeignKey(x => x.RestaurantId).OnDelete(DeleteBehavior.Restrict); } }