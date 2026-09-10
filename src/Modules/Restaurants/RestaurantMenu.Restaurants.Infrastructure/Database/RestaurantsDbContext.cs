using System.Reflection.Emit;

using Microsoft.EntityFrameworkCore;

using Npgsql;

using RestaurantMenu.Restaurants.Application.Abstractions.Data;

using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.DiningTables;
using RestaurantMenu.Restaurants.Domain.Memberships;
using RestaurantMenu.Restaurants.Domain.PublicMenuCodes;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Infrastructure.Database;

public sealed class RestaurantsDbContext
    : DbContext,
      IUnitOfWork
{
    public RestaurantsDbContext(
        DbContextOptions<RestaurantsDbContext> options)
        : base(
            options ??
            throw new ArgumentNullException(nameof(options)))
    {
    }

    public DbSet<Restaurant> Restaurants =>
        Set<Restaurant>();

    public DbSet<RestaurantMembership> RestaurantMemberships =>
        Set<RestaurantMembership>();
    public DbSet<BranchMembership> BranchMemberships => Set<BranchMembership>();
    public DbSet<MembershipInvitation> MembershipInvitations => Set<MembershipInvitation>();
    public DbSet<MembershipAuditEntry> MembershipAuditEntries => Set<MembershipAuditEntry>();

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<DiningTable> DiningTables => Set<DiningTable>();
    public DbSet<PublicMenuCode> PublicMenuCodes => Set<PublicMenuCode>();

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_membership_invitations_active_email" })
        { throw new ActiveInvitationExistsException("Active invitation already exists.", exception); }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "ux_public_menu_codes_code_hash"
            })
        {
            throw new PublicMenuCodeHashAlreadyExistsException(
                "Public menu code hash already exists.", exception);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "ux_dining_tables_branch_id_number"
            })
        {
            throw new DiningTableNumberAlreadyExistsException(
                "Dining table number already exists for the branch.", exception);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "ux_branches_restaurant_id_slug"
            })
        {
            throw new BranchSlugAlreadyExistsException(
                "Branch slug already exists for the restaurant.", exception);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "ux_restaurants_slug"
            })
        {
            throw new SlugAlreadyExistsException("Restaurant slug already exists.", exception);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyException(
                "A concurrent database update was detected.",
                exception);
        }
    }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("restaurants");

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(RestaurantsDbContext).Assembly);
    }
}