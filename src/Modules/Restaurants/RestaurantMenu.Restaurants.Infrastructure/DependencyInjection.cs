using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Infrastructure.Database;
using RestaurantMenu.Restaurants.Infrastructure.Restaurants;
using RestaurantMenu.Restaurants.Application.Restaurants.GetRestaurant;

using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Restaurants.CreateRestaurant;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

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

        services.AddScoped<
            IRestaurantReadService,
            RestaurantReadService>();

        services.AddScoped<IUnitOfWork>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    RestaurantsDbContext>());

        services.AddScoped<
            ICommandHandler<
                CreateRestaurantCommand,
                Result<RestaurantId>>,
            CreateRestaurantCommandHandler>();

        services.AddScoped<
            IQueryHandler<
                GetRestaurantQuery,
                Result<RestaurantResponse>>,
            GetRestaurantQueryHandler>();

        services.AddSingleton(TimeProvider.System);

        return services;
    }
}