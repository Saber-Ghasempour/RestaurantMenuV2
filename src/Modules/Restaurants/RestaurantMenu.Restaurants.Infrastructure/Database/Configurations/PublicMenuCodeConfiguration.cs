using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.DiningTables;
using RestaurantMenu.Restaurants.Domain.PublicMenuCodes;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Infrastructure.Database.Configurations;
internal sealed class PublicMenuCodeConfiguration : IEntityTypeConfiguration<PublicMenuCode>
{
    public void Configure(EntityTypeBuilder<PublicMenuCode> builder)
    {
        builder.ToTable("public_menu_codes", table => table.HasCheckConstraint(
            "ck_public_menu_codes_purpose_scope",
            "(purpose = 'MenuOnly' AND dining_table_id IS NULL) OR (purpose = 'DineInOrdering' AND branch_id IS NOT NULL AND dining_table_id IS NOT NULL)"));
        builder.HasKey(code => code.Id);
        builder.Property(code => code.Id).HasConversion(id => id.Value, value => new PublicMenuCodeId(value)).HasColumnName("id").ValueGeneratedNever();
        builder.Property(code => code.CodeHash).HasColumnName("code_hash").HasMaxLength(PublicMenuCode.MaxCodeHashLength).IsRequired();
        builder.Property(code => code.RestaurantId).HasConversion(id => id.Value, value => new RestaurantId(value)).HasColumnName("restaurant_id").ValueGeneratedNever();
        builder.Property(code => code.BranchId).HasConversion(id => id!.Value.Value, value => new BranchId(value)).HasColumnName("branch_id");
        builder.Property(code => code.DiningTableId).HasConversion(id => id!.Value.Value, value => new DiningTableId(value)).HasColumnName("dining_table_id");
        builder.Property(code => code.Purpose).HasConversion<string>().HasMaxLength(24).HasColumnName("purpose").IsRequired();
        builder.Property(code => code.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(code => code.ExpiresAtUtc).HasColumnName("expires_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(code => code.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(code => code.RotatedAtUtc).HasColumnName("rotated_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(code => code.Version).HasColumnName("version").HasDefaultValue(1L).IsConcurrencyToken().IsRequired();
        builder.HasOne<Restaurant>().WithMany().HasForeignKey(code => code.RestaurantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(code => new { code.RestaurantId, code.BranchId })
            .HasPrincipalKey(branch => new { branch.RestaurantId, branch.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DiningTable>().WithMany().HasForeignKey(code => new { code.RestaurantId, code.BranchId, code.DiningTableId })
            .HasPrincipalKey(table => new { table.RestaurantId, table.BranchId, table.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(code => code.CodeHash).IsUnique().HasDatabaseName("ux_public_menu_codes_code_hash");
        builder.HasIndex(code => new { code.RestaurantId, code.CreatedAtUtc, code.Id }).HasDatabaseName("ix_public_menu_codes_restaurant_created_id");
        builder.HasIndex(code => code.ExpiresAtUtc).HasDatabaseName("ix_public_menu_codes_expires_at_utc");
        builder.Ignore(code => code.DomainEvents);
    }
}
