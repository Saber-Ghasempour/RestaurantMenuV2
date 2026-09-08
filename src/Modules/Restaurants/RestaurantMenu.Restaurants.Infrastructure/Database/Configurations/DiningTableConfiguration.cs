using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.DiningTables;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Infrastructure.Database.Configurations;
internal sealed class DiningTableConfiguration : IEntityTypeConfiguration<DiningTable>
{
    public void Configure(EntityTypeBuilder<DiningTable> builder)
    {
        builder.ToTable("dining_tables");
        builder.HasKey(table => table.Id);
        builder.HasAlternateKey(table => new { table.RestaurantId, table.BranchId, table.Id });
        builder.Property(table => table.Id).HasConversion(id => id.Value, value => new DiningTableId(value)).HasColumnName("id").ValueGeneratedNever();
        builder.Property(table => table.RestaurantId).HasConversion(id => id.Value, value => new RestaurantId(value)).HasColumnName("restaurant_id").ValueGeneratedNever();
        builder.Property(table => table.BranchId).HasConversion(id => id.Value, value => new BranchId(value)).HasColumnName("branch_id").ValueGeneratedNever();
        builder.Property(table => table.Number).HasColumnName("number").IsRequired();
        builder.Property(table => table.DisplayName).HasColumnName("display_name").HasMaxLength(DiningTable.MaxDisplayNameLength);
        builder.Property(table => table.Capacity).HasColumnName("capacity");
        builder.Property(table => table.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(table => table.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(table => table.Version).HasColumnName("version").HasDefaultValue(1L).IsConcurrencyToken().IsRequired();
        builder.HasOne<Branch>().WithMany().HasForeignKey(table => new { table.RestaurantId, table.BranchId })
            .HasPrincipalKey(branch => new { branch.RestaurantId, branch.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(table => new { table.BranchId, table.Number }).IsUnique().HasDatabaseName("ux_dining_tables_branch_id_number");
        builder.HasIndex(table => new { table.RestaurantId, table.BranchId, table.Number, table.Id })
            .HasDatabaseName("ix_dining_tables_restaurant_branch_number_id");
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_dining_tables_number_positive", "number > 0");
            table.HasCheckConstraint("ck_dining_tables_capacity_positive", "capacity IS NULL OR capacity > 0");
        });
        builder.Ignore(table => table.DomainEvents);
    }
}
