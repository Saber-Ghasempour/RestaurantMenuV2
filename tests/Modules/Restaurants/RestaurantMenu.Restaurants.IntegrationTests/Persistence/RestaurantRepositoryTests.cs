using Microsoft.EntityFrameworkCore;

using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.Restaurants.Infrastructure.Database;
using RestaurantMenu.Restaurants.Infrastructure.Restaurants;

using Testcontainers.PostgreSql;

namespace RestaurantMenu.Restaurants.IntegrationTests.Persistence;

public sealed class RestaurantRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:18.6-alpine")
            .Build();

    public Task InitializeAsync()
    {
        return _postgres.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _postgres.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task AddShouldPersistRestaurant()
    {
        var options =
            new DbContextOptionsBuilder<RestaurantsDbContext>()
                .UseNpgsql(
                    _postgres.GetConnectionString(),
                    npgsqlOptions => 
                        npgsqlOptions.MigrationsHistoryTable(
                            "__ef_migrations_history",
                            "restaurants"))
                .Options;

        var restaurantId = RestaurantId.New();

        var result = Restaurant.Create(
            restaurantId,
            "Integration Test Restaurant",
            new DateTimeOffset(
                2026,
                9,
                6,
                12,
                0,
                0,
                TimeSpan.Zero));

        Assert.True(result.IsSuccess);

        await using (var context = new RestaurantsDbContext(options))
        {
            await context.Database.MigrateAsync();

            var repository = new RestaurantRepository(context);

            repository.Add(result.Value);

            await context.SaveChangesAsync();
        }

        await using var verificationContext =
            new RestaurantsDbContext(options);

        var persistedRestaurant =
            await verificationContext.Restaurants
                .AsNoTracking()
                .SingleAsync(
                    restaurant =>
                        restaurant.Id == restaurantId);

        Assert.Equal(
            restaurantId,
            persistedRestaurant.Id);

        Assert.Equal(
            "Integration Test Restaurant",
            persistedRestaurant.Name);
    }
}