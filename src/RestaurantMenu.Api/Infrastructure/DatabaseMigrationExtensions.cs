using Microsoft.EntityFrameworkCore;

using RestaurantMenu.Catalog.Infrastructure.Database;
using RestaurantMenu.Restaurants.Infrastructure.Database;
using RestaurantMenu.Media.Infrastructure.Database;
using RestaurantMenu.Ordering.Infrastructure.Database;
using RestaurantMenu.Feedback.Infrastructure.Database;

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
        var mediaDbContext = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        var orderingDbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var feedbackDbContext = scope.ServiceProvider.GetRequiredService<FeedbackDbContext>();

        await restaurantsDbContext.Database.MigrateAsync();
        await catalogDbContext.Database.MigrateAsync();
        await mediaDbContext.Database.MigrateAsync();
        await orderingDbContext.Database.MigrateAsync();
        await feedbackDbContext.Database.MigrateAsync();
    }
}
