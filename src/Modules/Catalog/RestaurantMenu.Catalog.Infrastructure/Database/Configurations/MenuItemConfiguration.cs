using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;

namespace RestaurantMenu.Catalog.Infrastructure.Database.Configurations;

internal sealed class MenuItemConfiguration
    : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("menu_items");
        builder.HasKey(menuItem => menuItem.Id);

        builder.Property(menuItem => menuItem.Id)
            .HasConversion(
                menuItemId => menuItemId.Value,
                value => new MenuItemId(value))
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(menuItem => menuItem.RestaurantId)
            .HasColumnName("restaurant_id")
            .IsRequired();

        builder.Property(menuItem => menuItem.CategoryId)
            .HasConversion(
                categoryId => categoryId.Value,
                value => new MenuCategoryId(value))
            .HasColumnName("category_id")
            .IsRequired();

        builder.Property(menuItem => menuItem.Name)
            .HasColumnName("name")
            .HasMaxLength(MenuItem.MaxNameLength)
            .IsRequired();

        builder.Property(menuItem => menuItem.Description)
            .HasColumnName("description")
            .HasMaxLength(MenuItem.MaxDescriptionLength);

        builder.OwnsOne(
            menuItem => menuItem.Price,
            priceBuilder =>
            {
                priceBuilder.Property(price => price.Amount)
                    .HasColumnName("price_amount")
                    .HasPrecision(18, 2)
                    .IsRequired();

                priceBuilder.Property(price => price.Currency)
                    .HasColumnName("price_currency")
                    .HasColumnType("character(3)")
                    .IsRequired();
            });

        builder.Navigation(menuItem => menuItem.Price)
            .IsRequired();

        builder.Property(menuItem => menuItem.DisplayOrder)
            .HasColumnName("display_order")
            .IsRequired();

        builder.Property(menuItem => menuItem.IsAvailable)
            .HasColumnName("is_available")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(menuItem => menuItem.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(menuItem => menuItem.Version)
            .HasColumnName("version")
            .HasDefaultValue(1L)
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasOne<MenuCategory>()
            .WithMany()
            .HasForeignKey(menuItem => menuItem.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(menuItem => new
            {
                menuItem.RestaurantId,
                menuItem.CategoryId,
                menuItem.DisplayOrder
            })
            .HasDatabaseName(
                "ix_menu_items_restaurant_category_display_order");

        builder.Ignore(menuItem => menuItem.DomainEvents);
    }
}
