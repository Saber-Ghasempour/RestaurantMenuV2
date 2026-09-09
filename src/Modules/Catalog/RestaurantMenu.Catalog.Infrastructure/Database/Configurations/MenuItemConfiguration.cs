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

        builder.Property(menuItem => menuItem.Recipe)
            .HasColumnName("recipe")
            .HasMaxLength(MenuItem.MaxRecipeLength);

        builder.Property(menuItem => menuItem.Calories)
            .HasColumnName("calories");

        builder.Property(menuItem => menuItem.Tags)
            .HasColumnName("tags")
            .HasColumnType("text[]")
            .HasDefaultValueSql("ARRAY[]::text[]")
            .IsRequired();

        builder.Property(menuItem => menuItem.AllergenNotes)
            .HasColumnName("allergen_notes")
            .HasMaxLength(MenuItem.MaxAllergenNotesLength);

        builder.Property(menuItem => menuItem.PreparationTimeMinutes)
            .HasColumnName("preparation_time_minutes");

        builder.Property(menuItem => menuItem.IsFeatured)
            .HasColumnName("is_featured")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(menuItem => menuItem.IsPublished)
            .HasColumnName("is_published")
            .HasDefaultValue(false)
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

        builder.Property(menuItem => menuItem.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(menuItem => menuItem.DeletedAtUtc)
            .HasColumnName("deleted_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.HasQueryFilter(menuItem => !menuItem.IsDeleted);

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

        builder.HasAlternateKey(menuItem => new
            {
                menuItem.RestaurantId,
                menuItem.Id
            });

        builder.HasIndex(menuItem => menuItem.Tags)
            .HasMethod("gin")
            .HasDatabaseName("ix_menu_items_tags_gin");

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_menu_items_calories_non_negative",
                "calories IS NULL OR calories >= 0");
            tableBuilder.HasCheckConstraint(
                "ck_menu_items_preparation_time_minutes_range",
                $"preparation_time_minutes IS NULL OR (preparation_time_minutes >= 1 AND preparation_time_minutes <= {MenuItem.MaxPreparationTimeMinutes})");
        });

        builder.Ignore(menuItem => menuItem.DomainEvents);
    }
}
