using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Infrastructure.Database;
using RestaurantMenu.Restaurants.Infrastructure.Database;
using RestaurantMenu.Restaurants.Domain.Restaurants;

using Testcontainers.PostgreSql;

namespace RestaurantMenu.Api.FunctionalTests.Infrastructure;

public sealed class TestWebApplicationFactory
    : WebApplicationFactory<Program>,
      IAsyncLifetime
{
    private const string RestaurantsConnectionStringVariable =
        "ConnectionStrings__Restaurants";

    private const string CatalogConnectionStringVariable =
        "ConnectionStrings__Catalog";

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
            var previousRestaurantsConnectionString =
                Environment.GetEnvironmentVariable(
                    RestaurantsConnectionStringVariable);
            var previousCatalogConnectionString =
                Environment.GetEnvironmentVariable(
                    CatalogConnectionStringVariable);
            var connectionString =
                _postgres.GetConnectionString();

            Environment.SetEnvironmentVariable(
                RestaurantsConnectionStringVariable,
                connectionString);
            Environment.SetEnvironmentVariable(
                CatalogConnectionStringVariable,
                connectionString);

            try
            {
                return base.CreateHost(builder);
            }
            finally
            {
                Environment.SetEnvironmentVariable(
                    RestaurantsConnectionStringVariable,
                    previousRestaurantsConnectionString);
                Environment.SetEnvironmentVariable(
                    CatalogConnectionStringVariable,
                    previousCatalogConnectionString);
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

    public async Task MigrateCatalogDatabaseAsync()
    {
        await using var scope =
            Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();

        await dbContext.Database.MigrateAsync();
    }

    public async Task<MenuCategory?> FindMenuCategoryAsync(
        Guid categoryId)
    {
        await using var scope =
            Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();
        var id = new MenuCategoryId(categoryId);

        return await dbContext.MenuCategories
            .AsNoTracking()
            .SingleOrDefaultAsync(
                category => category.Id == id);
    }

    public async Task<int> CountMenuCategoriesAsync()
    {
        await using var scope =
            Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();

        return await dbContext.MenuCategories.CountAsync();
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

    public async Task<Restaurant?> FindRestaurantIncludingDeletedAsync(
        Guid restaurantId)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                RestaurantsDbContext>();
        var id = new RestaurantId(restaurantId);

        return await dbContext.Restaurants
            .IgnoreQueryFilters()
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
