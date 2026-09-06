using Microsoft.EntityFrameworkCore;

using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.Restaurants.Infrastructure.Database;
using RestaurantMenu.Restaurants.Infrastructure.Restaurants;

using Testcontainers.PostgreSql;

namespace RestaurantMenu.Restaurants.IntegrationTests.Persistence;

public sealed class RestaurantReadServiceTests
    : IAsyncLifetime
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 9, 6, 17, 0, 0, TimeSpan.Zero);

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
    public async Task GetByIdShouldReturnRestaurantProjection()
    {
        var options = CreateOptions();
        var restaurantId = RestaurantId.New();

        var restaurantResult =
            Restaurant.Create(
                restaurantId,
                "Read Service Restaurant",
                CreatedAtUtc);

        Assert.True(restaurantResult.IsSuccess);

        await using (var arrangeContext =
                     new RestaurantsDbContext(options))
        {
            await arrangeContext.Database.MigrateAsync();

            arrangeContext.Restaurants.Add(
                restaurantResult.Value);

            await arrangeContext.SaveChangesAsync();
        }

        await using var queryContext =
            new RestaurantsDbContext(options);

        var readService =
            new RestaurantReadService(queryContext);

        var response =
            await readService.GetByIdAsync(
                restaurantId,
                CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(restaurantId.Value, response.Id);
        Assert.Equal(
            "Read Service Restaurant",
            response.Name);
        Assert.Equal(CreatedAtUtc, response.CreatedAtUtc);
    }

    [Fact]
    public async Task GetByIdShouldReturnNullWhenRestaurantDoesNotExist()
    {
        var options = CreateOptions();

        await using var context =
            new RestaurantsDbContext(options);

        await context.Database.MigrateAsync();

        var readService =
            new RestaurantReadService(context);

        var response =
            await readService.GetByIdAsync(
                RestaurantId.New(),
                CancellationToken.None);

        Assert.Null(response);
    }

    private DbContextOptions<RestaurantsDbContext>
        CreateOptions()
    {
        return new DbContextOptionsBuilder<
                RestaurantsDbContext>()
            .UseNpgsql(
                _postgres.GetConnectionString(),
                npgsqlOptions =>
                    npgsqlOptions.MigrationsHistoryTable(
                        "__ef_migrations_history",
                        "restaurants"))
            .Options;
    }
}