using Microsoft.EntityFrameworkCore;

using RestaurantMenu.Application.Abstractions.Data;
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

    [Fact]
    public async Task SaveChangesShouldThrowWhenRestaurantWasConcurrentlyUpdated()
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
            "Concurrent Restaurant",
            DateTimeOffset.UtcNow);

        Assert.True(result.IsSuccess);

        await using (var arrangeContext =
                     new RestaurantsDbContext(options))
        {
            await arrangeContext.Database.MigrateAsync();
            arrangeContext.Restaurants.Add(result.Value);
            await arrangeContext.SaveChangesAsync();
        }

        await using var firstContext =
            new RestaurantsDbContext(options);
        await using var secondContext =
            new RestaurantsDbContext(options);

        var firstRestaurant =
            await new RestaurantRepository(firstContext)
                .GetByIdAsync(
                    restaurantId,
                    CancellationToken.None);
        var secondRestaurant =
            await new RestaurantRepository(secondContext)
                .GetByIdAsync(
                    restaurantId,
                    CancellationToken.None);

        Assert.NotNull(firstRestaurant);
        Assert.NotNull(secondRestaurant);
        Assert.True(
            firstRestaurant.Rename("First Update").IsSuccess);
        Assert.True(
            secondRestaurant.Rename("Second Update").IsSuccess);

        await firstContext.SaveChangesAsync();

        await Assert.ThrowsAsync<ConcurrencyException>(
            () => secondContext.SaveChangesAsync());
    }

    [Fact]
    public async Task DeletedRestaurantShouldBeHiddenButRemainPersisted()
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
        var deletedAtUtc = new DateTimeOffset(
            2026,
            9,
            6,
            23,
            0,
            0,
            TimeSpan.Zero);
        var result = Restaurant.Create(
            restaurantId,
            "Soft Deleted Restaurant",
            deletedAtUtc.AddHours(-1));
        Assert.True(result.IsSuccess);

        await using (var context =
                     new RestaurantsDbContext(options))
        {
            await context.Database.MigrateAsync();
            context.Restaurants.Add(result.Value);
            await context.SaveChangesAsync();

            result.Value.Delete(deletedAtUtc);
            await context.SaveChangesAsync();
        }

        await using var verificationContext =
            new RestaurantsDbContext(options);

        Assert.Null(
            await verificationContext.Restaurants
                .SingleOrDefaultAsync(
                    restaurant =>
                        restaurant.Id == restaurantId));

        var persistedRestaurant =
            await verificationContext.Restaurants
                .IgnoreQueryFilters()
                .SingleAsync(
                    restaurant =>
                        restaurant.Id == restaurantId);

        Assert.True(persistedRestaurant.IsDeleted);
        Assert.Equal(
            deletedAtUtc,
            persistedRestaurant.DeletedAtUtc);
        Assert.Equal(2, persistedRestaurant.Version);
    }
}
