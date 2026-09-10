using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Memberships;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Infrastructure.Database.Configurations;

internal sealed class BranchMembershipConfiguration : IEntityTypeConfiguration<BranchMembership>
{
    public void Configure(EntityTypeBuilder<BranchMembership> builder)
    {
        builder.ToTable("branch_memberships", table =>
        {
            table.HasCheckConstraint("ck_branch_memberships_role", "role IN ('Manager', 'Cashier', 'Kitchen', 'Waiter')");
            table.HasCheckConstraint("ck_branch_memberships_status", "status IN ('Active', 'Suspended')");
        });
        builder.HasKey(value => new { value.BranchId, value.Subject });
        builder.Property(value => value.RestaurantId).HasConversion(id => id.Value,
            value => new RestaurantId(value)).HasColumnName("restaurant_id").ValueGeneratedNever();
        builder.Property(value => value.BranchId).HasConversion(id => id.Value,
            value => new BranchId(value)).HasColumnName("branch_id").ValueGeneratedNever();
        builder.Property(value => value.Subject).HasColumnName("subject")
            .HasMaxLength(RestaurantMembership.MaxSubjectLength).IsRequired();
        builder.Property(value => value.Role).HasConversion<string>().HasColumnName("role").HasMaxLength(32).IsRequired();
        builder.Property(value => value.Status).HasConversion<string>().HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(value => value.Version).HasColumnName("version").HasDefaultValue(1L).IsConcurrencyToken().IsRequired();
        builder.HasOne<Branch>().WithMany().HasForeignKey(value => new { value.RestaurantId, value.BranchId })
            .HasPrincipalKey(branch => new { branch.RestaurantId, branch.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RestaurantMembership>().WithMany().HasForeignKey(value => new { value.RestaurantId, value.Subject })
            .HasPrincipalKey(member => new { member.RestaurantId, member.Subject }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(value => new { value.RestaurantId, value.Subject, value.Status })
            .HasDatabaseName("ix_branch_memberships_restaurant_subject_status");
    }
}
