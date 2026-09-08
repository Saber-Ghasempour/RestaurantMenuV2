using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using StackExchange.Redis;

using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Abstractions.Caching;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Infrastructure.Caching;
using RestaurantMenu.Restaurants.Infrastructure.Database;
using RestaurantMenu.Restaurants.Infrastructure.Restaurants;
using RestaurantMenu.Restaurants.Infrastructure.Memberships;
using RestaurantMenu.Restaurants.Application.Restaurants.GetRestaurant;
using RestaurantMenu.Restaurants.Application.Restaurants.ListRestaurants;
using RestaurantMenu.Restaurants.Application.Restaurants.UpdateRestaurant;

using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Restaurants.CreateRestaurant;
using RestaurantMenu.Restaurants.Application.Restaurants.DeleteRestaurant;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;
using RestaurantMenu.Restaurants.Application.Restaurants.UpdateRestaurantProfile;

namespace RestaurantMenu.Restaurants.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRestaurantsInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string redisConnectionString,
        TimeSpan cacheTimeToLive,
        int redisConnectTimeoutMilliseconds,
        int redisOperationTimeoutMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(redisConnectionString);

        if (cacheTimeToLive <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cacheTimeToLive),
                cacheTimeToLive,
                "Cache time-to-live must be positive.");
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            redisConnectTimeoutMilliseconds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            redisOperationTimeoutMilliseconds);

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

        services.AddScoped<RestaurantMembershipRepository>();
        services.AddScoped<IRestaurantMembershipRepository>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    RestaurantMembershipRepository>());
        services.AddScoped<IRestaurantMembershipReadService>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    RestaurantMembershipRepository>());

        var redisConfiguration =
            ConfigurationOptions.Parse(redisConnectionString);
        redisConfiguration.AbortOnConnectFail = false;
        redisConfiguration.BacklogPolicy = BacklogPolicy.FailFast;
        redisConfiguration.ConnectRetry = 2;
        redisConfiguration.ConnectTimeout =
            redisConnectTimeoutMilliseconds;
        redisConfiguration.AsyncTimeout =
            redisOperationTimeoutMilliseconds;
        redisConfiguration.SyncTimeout =
            redisOperationTimeoutMilliseconds;
        redisConfiguration.ReconnectRetryPolicy =
            new LinearRetry(500);

        services.AddStackExchangeRedisCache(
            options =>
            {
                options.ConfigurationOptions = redisConfiguration;
                options.InstanceName = "restaurant-menu:v1:";
            });
        services.AddSingleton(
            new RestaurantCacheOptions(cacheTimeToLive));
        services.AddSingleton<
            IRestaurantCache,
            RestaurantCache>();

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

        services.AddScoped<
            IQueryHandler<
                ListRestaurantsQuery,
                Result<RestaurantsPage>>,
            ListRestaurantsQueryHandler>();

        services.AddScoped<
            ICommandHandler<
                UpdateRestaurantCommand,
                Result<long>>,
            UpdateRestaurantCommandHandler>();

        services.AddScoped<
            ICommandHandler<
                DeleteRestaurantCommand,
                Result<RestaurantId>>,
            DeleteRestaurantCommandHandler>();

        services.AddScoped<
            ICommandHandler<
                UpdateRestaurantProfileCommand,
                Result<long>>,
            UpdateRestaurantProfileCommandHandler>();

        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
