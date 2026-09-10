using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantMenu.Restaurants.Domain.Memberships;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Infrastructure.Database.Configurations;

internal sealed class RestaurantMembershipConfiguration
    : IEntityTypeConfiguration<RestaurantMembership>
{
    public void Configure(
        EntityTypeBuilder<RestaurantMembership> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("restaurant_memberships");

        builder.HasKey(
            membership => new
            {
                membership.RestaurantId,
                membership.Subject
            });

        builder.Property(membership => membership.RestaurantId)
            .HasConversion(
                restaurantId => restaurantId.Value,
                value => new RestaurantId(value))
            .HasColumnName("restaurant_id")
            .ValueGeneratedNever();

        builder.Property(membership => membership.Subject)
            .HasColumnName("subject")
            .HasMaxLength(RestaurantMembership.MaxSubjectLength)
            .IsRequired();

        builder.Property(membership => membership.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(24).HasDefaultValue(RestaurantMembershipStatus.Active).IsRequired();
        builder.Property(x => x.Version).HasColumnName("version").HasDefaultValue(1L).IsConcurrencyToken().IsRequired();

        builder.Property(membership => membership.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(membership => membership.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(membership => membership.Subject)
            .HasDatabaseName("ix_restaurant_memberships_subject");
    }
}