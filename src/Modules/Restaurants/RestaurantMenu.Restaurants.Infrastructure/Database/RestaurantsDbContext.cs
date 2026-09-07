using System.Reflection.Emit;

using Microsoft.EntityFrameworkCore;

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
