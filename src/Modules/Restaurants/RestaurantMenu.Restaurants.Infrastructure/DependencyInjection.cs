using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Infrastructure.Database;
using RestaurantMenu.Restaurants.Infrastructure.Restaurants;

namespace RestaurantMenu.Restaurants.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRestaurantsInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<RestaurantsDbContext>(
            options =>
                options.UseNpgsql(
                    connectionString,
                    npgsqlOptions =>
                        npgsqlOptions.MigrationsHistoryTable(
                            "__ef_migrations_history",
                            "restaurants")));

        services.AddScoped<
            IRestaurantRepository,
            RestaurantRepository>();

        services.AddScoped<IUnitOfWork>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    RestaurantsDbContext>());

        return services;
    }
}