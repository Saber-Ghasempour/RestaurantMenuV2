using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantMenu.Payments.Domain.Profiles;

namespace RestaurantMenu.Payments.Infrastructure.Database;

public sealed class ProfileConfiguration : IEntityTypeConfiguration<RestaurantPaymentProfile>
{
    public void Configure(EntityTypeBuilder<RestaurantPaymentProfile> builder)
    {
        builder.ToTable("restaurant_payment_profiles", table =>
        {
            table.HasCheckConstraint("ck_payment_profiles_country", "country ~ '^[A-Z]{2}$'");
            table.HasCheckConstraint("ck_payment_profiles_currency", "currency ~ '^[A-Z]{3}$'");
            table.HasCheckConstraint("ck_payment_profiles_commission", "commission_rate_basis_points BETWEEN 1 AND 10000");
        });
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(profile => profile.RestaurantId).HasColumnName("restaurant_id").IsRequired();
        builder.Property(profile => profile.StripeAccountId).HasColumnName("stripe_account_id").HasMaxLength(255).IsRequired();
        builder.Property(profile => profile.Country).HasColumnName("country").HasColumnType("character(2)").IsRequired();
        builder.Property(profile => profile.Currency).HasColumnName("currency").HasColumnType("character(3)").IsRequired();
        builder.Property(profile => profile.CommissionRateBasisPoints).HasColumnName("commission_rate_basis_points").IsRequired();
        builder.Property(profile => profile.IsEnabled).HasColumnName("is_enabled").IsRequired();
        builder.Property(profile => profile.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(profile => profile.Version).HasColumnName("version").HasDefaultValue(1L).IsConcurrencyToken().IsRequired();
        builder.HasIndex(profile => profile.RestaurantId).IsUnique().HasDatabaseName("ux_payment_profiles_restaurant");
        builder.HasIndex(profile => profile.StripeAccountId).IsUnique().HasDatabaseName("ux_payment_profiles_stripe_account");
        builder.Ignore(profile => profile.DomainEvents);
    }
}
