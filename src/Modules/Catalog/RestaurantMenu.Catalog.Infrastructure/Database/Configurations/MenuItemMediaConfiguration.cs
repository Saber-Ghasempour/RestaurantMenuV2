using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantMenu.Catalog.Domain.Items;

namespace RestaurantMenu.Catalog.Infrastructure.Database.Configurations;

internal sealed class MenuItemMediaConfiguration : IEntityTypeConfiguration<MenuItemMedia>
{
    public void Configure(EntityTypeBuilder<MenuItemMedia> builder)
    {
        builder.ToTable("menu_item_media");
        builder.HasKey(x => new { x.MenuItemId, x.MediaAssetId });
        builder.Property(x => x.RestaurantId).HasColumnName("restaurant_id");
        builder.Property(x => x.MenuItemId).HasConversion(x => x.Value, x => new MenuItemId(x)).HasColumnName("menu_item_id");
        builder.Property(x => x.MediaAssetId).HasColumnName("media_asset_id");
        builder.Property(x => x.DisplayOrder).HasColumnName("display_order");
        builder.Property(x => x.AltText).HasColumnName("alt_text").HasMaxLength(MenuItemMedia.MaxAltTextLength);
        builder.Property(x => x.IsPrimary).HasColumnName("is_primary");
        builder.HasIndex(x => new { x.RestaurantId, x.MenuItemId, x.DisplayOrder }).IsUnique().HasDatabaseName("ux_menu_item_media_order");
        builder.HasIndex(x => new { x.RestaurantId, x.MenuItemId }).IsUnique().HasFilter("is_primary").HasDatabaseName("ux_menu_item_media_primary");
        builder.HasOne<MenuItem>().WithMany().HasForeignKey(x => new { x.RestaurantId, x.MenuItemId })
            .HasPrincipalKey(x => new { x.RestaurantId, x.Id }).OnDelete(DeleteBehavior.Cascade);
    }
}
