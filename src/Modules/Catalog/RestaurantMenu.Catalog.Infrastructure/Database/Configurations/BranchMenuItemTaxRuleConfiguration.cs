using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Taxation;

namespace RestaurantMenu.Catalog.Infrastructure.Database.Configurations;

internal sealed class BranchMenuItemTaxRuleConfiguration
    : IEntityTypeConfiguration<BranchMenuItemTaxRule>
{
    public void Configure(EntityTypeBuilder<BranchMenuItemTaxRule> builder)
    {
        builder.ToTable("branch_menu_item_tax_rules", table =>
        {
            table.HasCheckConstraint("ck_branch_menu_item_tax_rules_rate",
                "rate_basis_points BETWEEN 0 AND 10000");
            table.HasCheckConstraint("ck_branch_menu_item_tax_rules_behavior",
                "behavior IN ('Inclusive', 'Exclusive')");
        });
        builder.HasKey(rule => new { rule.BranchId, rule.MenuItemId });
        builder.Ignore(rule => rule.Id);
        builder.Property(rule => rule.RestaurantId).HasColumnName("restaurant_id").IsRequired();
        builder.Property(rule => rule.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(rule => rule.MenuItemId)
            .HasConversion(id => id.Value, value => new MenuItemId(value))
            .HasColumnName("menu_item_id").IsRequired();
        builder.Property(rule => rule.RateBasisPoints).HasColumnName("rate_basis_points").IsRequired();
        builder.Property(rule => rule.Behavior).HasConversion<string>()
            .HasColumnName("behavior").HasMaxLength(16).IsRequired();
        builder.Property(rule => rule.CreatedAtUtc).HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(rule => rule.Version).HasColumnName("version")
            .HasDefaultValue(1L).IsConcurrencyToken().IsRequired();
        builder.HasOne<MenuItem>().WithMany()
            .HasForeignKey(rule => new { rule.RestaurantId, rule.MenuItemId })
            .HasPrincipalKey(item => new { item.RestaurantId, item.Id })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(rule => new { rule.RestaurantId, rule.BranchId })
            .HasDatabaseName("ix_branch_menu_item_tax_rules_restaurant_branch");
        builder.Ignore(rule => rule.DomainEvents);
    }
}
