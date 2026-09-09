using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Variants;

namespace RestaurantMenu.Catalog.Infrastructure.Database.Configurations;

internal sealed class MenuItemVariantConfiguration
    : IEntityTypeConfiguration<MenuItemVariant>
{
    public void Configure(EntityTypeBuilder<MenuItemVariant> builder)
    {
        builder.ToTable("menu_item_variants");
        builder.HasKey(variant => variant.Id);
        builder.Property(variant => variant.Id)
            .HasConversion(id => id.Value, value => new MenuItemVariantId(value))
            .HasColumnName("id").ValueGeneratedNever();
        builder.Property(variant => variant.RestaurantId)
            .HasColumnName("restaurant_id").IsRequired();
        builder.Property(variant => variant.MenuItemId)
            .HasConversion(id => id.Value, value => new MenuItemId(value))
            .HasColumnName("menu_item_id").IsRequired();
        builder.Property(variant => variant.Name)
            .HasColumnName("name").HasMaxLength(MenuItemVariant.MaxNameLength).IsRequired();
        builder.Property(variant => variant.Description)
            .HasColumnName("description").HasMaxLength(MenuItemVariant.MaxDescriptionLength);
        builder.OwnsOne(variant => variant.Price, price =>
        {
            price.Property(value => value.Amount)
                .HasColumnName("price_amount").HasPrecision(18, 2).IsRequired();
            price.Property(value => value.Currency)
                .HasColumnName("price_currency").HasColumnType("character(3)").IsRequired();
        });
        builder.Navigation(variant => variant.Price).IsRequired();
        builder.Property(variant => variant.DisplayOrder)
            .HasColumnName("display_order").IsRequired();
        builder.Property(variant => variant.IsDefault)
            .HasColumnName("is_default").HasDefaultValue(false).IsRequired();
        builder.Property(variant => variant.IsAvailable)
            .HasColumnName("is_available").HasDefaultValue(true).IsRequired();
        builder.Property(variant => variant.CreatedAtUtc)
            .HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(variant => variant.Version)
            .HasColumnName("version").HasDefaultValue(1L).IsConcurrencyToken().IsRequired();
        builder.Property(variant => variant.IsDeleted)
            .HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(variant => variant.DeletedAtUtc)
            .HasColumnName("deleted_at_utc").HasColumnType("timestamp with time zone");
        builder.HasQueryFilter(variant => !variant.IsDeleted);
        builder.HasOne<MenuItem>().WithMany()
            .HasForeignKey(variant => new { variant.RestaurantId, variant.MenuItemId })
            .HasPrincipalKey(item => new { item.RestaurantId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(variant => new
            { variant.RestaurantId, variant.MenuItemId, variant.DisplayOrder })
            .HasDatabaseName("ix_menu_item_variants_restaurant_item_display_order");
        builder.HasIndex(variant => new { variant.MenuItemId, variant.IsDefault })
            .IsUnique()
            .HasFilter("is_default = TRUE AND is_deleted = FALSE")
            .HasDatabaseName("ux_menu_item_variants_one_default");
        builder.HasIndex(variant => new { variant.MenuItemId, variant.Name })
            .IsUnique()
            .HasFilter("is_deleted = FALSE")
            .HasDatabaseName("ux_menu_item_variants_active_name");
        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "ck_menu_item_variants_display_order_non_negative", "display_order >= 0");
            table.HasCheckConstraint(
                "ck_menu_item_variants_price_non_negative", "price_amount >= 0");
            table.HasCheckConstraint(
                "ck_menu_item_variants_currency_format",
                "price_currency ~ '^[A-Z]{3}$'");
        });
        builder.Ignore(variant => variant.DomainEvents);
    }
}
