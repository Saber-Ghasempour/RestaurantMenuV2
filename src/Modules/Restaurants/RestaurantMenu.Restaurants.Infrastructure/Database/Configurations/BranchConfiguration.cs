using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Infrastructure.Database.Configurations;

internal sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("branches");
        builder.HasKey(branch => branch.Id);
        builder.HasAlternateKey(branch => new { branch.RestaurantId, branch.Id });
        builder.Property(branch => branch.Id)
            .HasConversion(id => id.Value, value => new BranchId(value))
            .HasColumnName("id").ValueGeneratedNever();
        builder.Property(branch => branch.RestaurantId)
            .HasConversion(id => id.Value, value => new RestaurantId(value))
            .HasColumnName("restaurant_id").ValueGeneratedNever();
        builder.Property(branch => branch.Name).HasColumnName("name")
            .HasMaxLength(Branch.MaxNameLength).IsRequired();
        builder.Property(branch => branch.Slug).HasColumnName("slug")
            .HasMaxLength(Branch.MaxSlugLength);
        builder.Property(branch => branch.Phone).HasColumnName("phone")
            .HasMaxLength(Branch.MaxPhoneLength);
        builder.Property(branch => branch.AddressLine).HasColumnName("address_line")
            .HasMaxLength(Branch.MaxAddressLength);
        builder.Property(branch => branch.CityName).HasColumnName("city_name")
            .HasMaxLength(Branch.MaxLocationNameLength);
        builder.Property(branch => branch.RegionName).HasColumnName("region_name")
            .HasMaxLength(Branch.MaxLocationNameLength);
        builder.Property(branch => branch.PostalCode).HasColumnName("postal_code")
            .HasMaxLength(Branch.MaxPostalCodeLength);
        builder.Property(branch => branch.CountryCode).HasColumnName("country_code")
            .HasMaxLength(2).IsFixedLength();
        builder.Property(branch => branch.Latitude).HasColumnName("latitude")
            .HasPrecision(9, 6);
        builder.Property(branch => branch.Longitude).HasColumnName("longitude")
            .HasPrecision(9, 6);
        builder.Property(branch => branch.TimeZoneId).HasColumnName("time_zone_id")
            .HasMaxLength(Branch.MaxTimeZoneIdLength);
        builder.Property(branch => branch.IsActive).HasColumnName("is_active")
            .HasDefaultValue(true).IsRequired();
        builder.Property(branch => branch.CreatedAtUtc).HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(branch => branch.Version).HasColumnName("version")
            .HasDefaultValue(1L).IsConcurrencyToken().IsRequired();
        builder.Property(branch => branch.IsDeleted).HasColumnName("is_deleted")
            .HasDefaultValue(false).IsRequired();
        builder.Property(branch => branch.DeletedAtUtc).HasColumnName("deleted_at_utc")
            .HasColumnType("timestamp with time zone");
        builder.HasOne<Restaurant>().WithMany()
            .HasForeignKey(branch => branch.RestaurantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(branch => branch.RestaurantId)
            .HasDatabaseName("ix_branches_restaurant_id");
        builder.HasIndex(branch => new { branch.RestaurantId, branch.Slug }).IsUnique()
            .HasDatabaseName("ux_branches_restaurant_id_slug");
        builder.HasIndex(branch => new { branch.RestaurantId, branch.Name, branch.Id })
            .HasDatabaseName("ix_branches_restaurant_id_name_id");
        builder.HasQueryFilter(branch => !branch.IsDeleted);
        builder.Ignore(branch => branch.DomainEvents);
    }
}
