using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using RestaurantMenu.Restaurants.Infrastructure.Database;
using RestaurantMenu.Restaurants.Domain.Restaurants;

using Testcontainers.PostgreSql;

namespace RestaurantMenu.Api.FunctionalTests.Infrastructure;

public sealed class TestWebApplicationFactory
    : WebApplicationFactory<Program>,
      IAsyncLifetime
{
    private const string ConnectionStringVariable =
        "ConnectionStrings__Restaurants";

    private static readonly object EnvironmentVariableLock =
        new();

    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:18.6-alpine")
            .Build();

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();

        Dispose();
    }

    protected override IHost CreateHost(
        IHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        lock (EnvironmentVariableLock)
        {
            var previousConnectionString =
                Environment.GetEnvironmentVariable(
                    ConnectionStringVariable);

            Environment.SetEnvironmentVariable(
                ConnectionStringVariable,
                _postgres.GetConnectionString());

            try
            {
                return base.CreateHost(builder);
            }
            finally
            {
                Environment.SetEnvironmentVariable(
                    ConnectionStringVariable,
                    previousConnectionString);
            }
        }
    }

    public async Task MigrateDatabaseAsync()
    {
        await using var scope =
            Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                RestaurantsDbContext>();

        await dbContext.Database.MigrateAsync();
    }

    public async Task<Restaurant?> FindRestaurantAsync(
        Guid restaurantId)
    {
        await using var scope =
            Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                RestaurantsDbContext>();

        var id = new RestaurantId(restaurantId);

        return await dbContext.Restaurants
            .AsNoTracking()
            .SingleOrDefaultAsync(
                restaurant => restaurant.Id == id);
    }

    public async Task<int> CountRestaurantsAsync()
    {
        await using var scope =
            Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                RestaurantsDbContext>();

        return await dbContext.Restaurants.CountAsync();
    }

    public async Task<Restaurant> SeedRestaurantAsync(
        string name,
        DateTimeOffset createdAtUtc)
    {
        await using var scope =
            Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                RestaurantsDbContext>();

        var result = Restaurant.Create(
            RestaurantId.New(),
            name,
            createdAtUtc);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Could not seed restaurant: {result.Error.Code}");
        }

        dbContext.Restaurants.Add(result.Value);

        await dbContext.SaveChangesAsync();

        return result.Value;
    }
}
