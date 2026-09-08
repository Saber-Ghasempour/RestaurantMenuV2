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
        builder.Property(restaurant => restaurant.WebsiteUrl)
            .HasColumnName("website_url").HasMaxLength(2048);
        builder.Property(restaurant => restaurant.InstagramUrl)
            .HasColumnName("instagram_url").HasMaxLength(2048);
        builder.Property(restaurant => restaurant.FacebookUrl)
            .HasColumnName("facebook_url").HasMaxLength(2048);
        builder.Property(restaurant => restaurant.WhatsAppUrl)
            .HasColumnName("whats_app_url").HasMaxLength(2048);
        builder.Property(restaurant => restaurant.TelegramUrl)
            .HasColumnName("telegram_url").HasMaxLength(2048);
        builder.Property(restaurant => restaurant.TwitterUrl)
            .HasColumnName("twitter_url").HasMaxLength(2048);
        builder.Property(restaurant => restaurant.Description)
            .HasColumnName("description").HasMaxLength(Restaurant.MaxDescriptionLength);
        builder.Property(restaurant => restaurant.About)
            .HasColumnName("about").HasMaxLength(Restaurant.MaxAboutLength);
        builder.Property(restaurant => restaurant.Address)
            .HasColumnName("address").HasMaxLength(Restaurant.MaxAddressLength);

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

        builder.Property(restaurant => restaurant.Version)
            .HasColumnName("version")
            .HasDefaultValue(1L)
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(restaurant => restaurant.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(restaurant => restaurant.DeletedAtUtc)
            .HasColumnName("deleted_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.HasQueryFilter(
            restaurant => !restaurant.IsDeleted);

        builder.Ignore(restaurant => restaurant.DomainEvents);
    }
}
