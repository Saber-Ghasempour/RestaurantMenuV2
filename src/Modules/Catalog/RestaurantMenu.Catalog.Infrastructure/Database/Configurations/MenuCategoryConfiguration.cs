using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantMenu.Catalog.Domain.Categories;

namespace RestaurantMenu.Catalog.Infrastructure.Database.Configurations;

internal sealed class MenuCategoryConfiguration
    : IEntityTypeConfiguration<MenuCategory>
{
    public void Configure(
        EntityTypeBuilder<MenuCategory> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("menu_categories");
        builder.HasKey(category => category.Id);

        builder.Property(category => category.Id)
            .HasConversion(
                categoryId => categoryId.Value,
                value => new MenuCategoryId(value))
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(category => category.RestaurantId)
            .HasColumnName("restaurant_id")
            .IsRequired();

        builder.HasIndex(
                category => new
                {
                    category.RestaurantId,
                    category.DisplayOrder
                })
            .HasDatabaseName(
                "ix_menu_categories_restaurant_id_display_order");

        builder.Property(category => category.ParentId)
            .HasConversion(
                parentId => parentId.HasValue
                    ? parentId.Value.Value
                    : (Guid?)null,
                value => value.HasValue
                    ? new MenuCategoryId(value.Value)
                    : null)
            .HasColumnName("parent_id");

        builder.Property(category => category.Name)
            .HasColumnName("name")
            .HasMaxLength(MenuCategory.MaxNameLength)
            .IsRequired();

        builder.Property(category => category.DisplayOrder)
            .HasColumnName("display_order")
            .IsRequired();

        builder.Property(category => category.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(category => category.Version)
            .HasColumnName("version")
            .HasDefaultValue(1L)
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(category => category.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(category => category.DeletedAtUtc)
            .HasColumnName("deleted_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.HasQueryFilter(category => !category.IsDeleted);

        builder.HasOne<MenuCategory>()
            .WithMany()
            .HasForeignKey(category => category.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(category => category.DomainEvents);
    }
}
