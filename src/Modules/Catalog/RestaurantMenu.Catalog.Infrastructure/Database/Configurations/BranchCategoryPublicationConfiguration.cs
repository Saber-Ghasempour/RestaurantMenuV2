using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Publications;

namespace RestaurantMenu.Catalog.Infrastructure.Database.Configurations;

internal sealed class BranchCategoryPublicationConfiguration
    : IEntityTypeConfiguration<BranchCategoryPublication>
{
    public void Configure(EntityTypeBuilder<BranchCategoryPublication> builder)
    {
        builder.ToTable(
            "branch_category_publications",
            tableBuilder => tableBuilder.HasCheckConstraint(
                "ck_branch_category_publications_display_order_override",
                "display_order_override IS NULL OR display_order_override >= 0"));
        builder.HasKey(publication => new
        {
            publication.BranchId,
            publication.CategoryId
        });

        builder.Ignore(publication => publication.Id);
        builder.Property(publication => publication.RestaurantId)
            .HasColumnName("restaurant_id")
            .IsRequired();
        builder.Property(publication => publication.BranchId)
            .HasColumnName("branch_id")
            .IsRequired();
        builder.Property(publication => publication.CategoryId)
            .HasConversion(
                id => id.Value,
                value => new MenuCategoryId(value))
            .HasColumnName("category_id")
            .IsRequired();
        builder.Property(publication => publication.IsPublished)
            .HasColumnName("is_published")
            .IsRequired();
        builder.Property(publication => publication.DisplayOrderOverride)
            .HasColumnName("display_order_override");
        builder.Property(publication => publication.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(publication => publication.Version)
            .HasColumnName("version")
            .HasDefaultValue(1L)
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasIndex(publication => new
            {
                publication.RestaurantId,
                publication.BranchId,
                publication.IsPublished,
                publication.DisplayOrderOverride
            })
            .HasDatabaseName("ix_branch_category_publications_public_menu");
        builder.HasOne<MenuCategory>()
            .WithMany()
            .HasForeignKey(publication => new
            {
                publication.RestaurantId,
                publication.CategoryId
            })
            .HasPrincipalKey(category => new
            {
                category.RestaurantId,
                category.Id
            })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(publication => new
            {
                publication.RestaurantId,
                publication.CategoryId
            })
            .HasDatabaseName(
                "ix_branch_category_publications_restaurant_id_category_id");
        builder.Ignore(publication => publication.DomainEvents);
    }
}
