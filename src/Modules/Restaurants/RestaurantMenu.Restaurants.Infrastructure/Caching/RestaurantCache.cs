using System.Text.Json;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

using RestaurantMenu.Restaurants.Application.Abstractions.Caching;
using RestaurantMenu.Restaurants.Application.Restaurants.GetRestaurant;
using RestaurantMenu.Restaurants.Domain.Restaurants;

using StackExchange.Redis;

namespace RestaurantMenu.Restaurants.Infrastructure.Caching;

public sealed partial class RestaurantCache : IRestaurantCache
{
    private const string KeyPrefix = "restaurants:";

    private readonly IDistributedCache _cache;
    private readonly RestaurantCacheOptions _options;
    private readonly ILogger<RestaurantCache> _logger;

    public RestaurantCache(
        IDistributedCache cache,
        RestaurantCacheOptions options,
        ILogger<RestaurantCache> logger)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _cache = cache;
        _options = options;
        _logger = logger;
    }

    public async Task<RestaurantResponse?> GetAsync(
        RestaurantId restaurantId,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = await _cache.GetAsync(
                GetKey(restaurantId),
                cancellationToken);

            return payload is null
                ? null
                : JsonSerializer.Deserialize(
                    payload,
                    RestaurantCacheJsonContext.Default.RestaurantResponse);
        }
        catch (RedisException exception)
        {
            LogReadFailure(
                _logger,
                restaurantId.Value,
                exception);

            return null;
        }
        catch (JsonException exception)
        {
            LogInvalidPayload(
                _logger,
                restaurantId.Value,
                exception);

            return null;
        }
    }

    public async Task SetAsync(
        RestaurantResponse restaurant,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(restaurant);

        var payload = JsonSerializer.SerializeToUtf8Bytes(
            restaurant,
            RestaurantCacheJsonContext.Default.RestaurantResponse);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _options.TimeToLive
        };

        try
        {
            await _cache.SetAsync(
                GetKey(new RestaurantId(restaurant.Id)),
                payload,
                options,
                cancellationToken);
        }
        catch (RedisException exception)
        {
            LogWriteFailure(
                _logger,
                restaurant.Id,
                exception);
        }
    }

    public async Task RemoveAsync(
        RestaurantId restaurantId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _cache.RemoveAsync(
                GetKey(restaurantId),
                cancellationToken);
        }
        catch (RedisException exception)
        {
            LogRemovalFailure(
                _logger,
                restaurantId.Value,
                exception);
        }
    }

    private static string GetKey(RestaurantId restaurantId) =>
        $"{KeyPrefix}{restaurantId.Value:N}";

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Warning,
        Message = "Redis read failed for restaurant {RestaurantId}; using the database fallback.")]
    private static partial void LogReadFailure(
        ILogger logger,
        Guid restaurantId,
        Exception exception);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Warning,
        Message = "Redis contained an invalid payload for restaurant {RestaurantId}; using the database fallback.")]
    private static partial void LogInvalidPayload(
        ILogger logger,
        Guid restaurantId,
        Exception exception);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Warning,
        Message = "Redis write failed for restaurant {RestaurantId}; the response was not cached.")]
    private static partial void LogWriteFailure(
        ILogger logger,
        Guid restaurantId,
        Exception exception);

    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Warning,
        Message = "Redis invalidation failed for restaurant {RestaurantId}; the cached value will expire by TTL.")]
    private static partial void LogRemovalFailure(
        ILogger logger,
        Guid restaurantId,
        Exception exception);
}
