using System.Reflection.Emit;

using Microsoft.EntityFrameworkCore;
using Npgsql;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;

using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Memberships;
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

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(
                cancellationToken);
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
