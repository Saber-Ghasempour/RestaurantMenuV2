using Microsoft.EntityFrameworkCore;

using RestaurantMenu.Catalog.Infrastructure.Database;
using RestaurantMenu.Restaurants.Infrastructure.Database;

namespace RestaurantMenu.Api.Infrastructure;

public static class DatabaseMigrationExtensions
{
    public static async Task ApplyDatabaseMigrationsAsync(
        this WebApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);

        await using var scope =
            application.Services.CreateAsyncScope();

        var restaurantsDbContext =
            scope.ServiceProvider.GetRequiredService<
                RestaurantsDbContext>();
        var catalogDbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();

        await restaurantsDbContext.Database.MigrateAsync();
        await catalogDbContext.Database.MigrateAsync();
    }
}
