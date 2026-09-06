using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Infrastructure.Database.Configurations;

internal sealed class RestaurantConfiguration
    : IEntityTypeConfiguration<Restaurant>
{
    public void Configure(
        EntityTypeBuilder<Restaurant> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("restaurants");

        builder.HasKey(restaurant => restaurant.Id);

        builder.Property(restaurant => restaurant.Id)
            .HasConversion(
                restaurantId => restaurantId.Value,
                value => new RestaurantId(value))
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(restaurant => restaurant.Name)
            .HasColumnName("name")
            .HasMaxLength(Restaurant.MaxNameLength)
            .IsRequired();

        builder.Property(restaurant => restaurant.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Ignore(restaurant => restaurant.DomainEvents);
    }
}