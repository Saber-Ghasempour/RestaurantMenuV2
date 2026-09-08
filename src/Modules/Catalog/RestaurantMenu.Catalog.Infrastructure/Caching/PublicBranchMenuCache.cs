using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using RestaurantMenu.Catalog.Application.Abstractions.Caching;
using RestaurantMenu.Catalog.Application.PublicMenus.GetPublicBranchMenu;
using StackExchange.Redis;

namespace RestaurantMenu.Catalog.Infrastructure.Caching;

public sealed partial class PublicBranchMenuCache(
    IDistributedCache cache,
    PublicBranchMenuCacheOptions options,
    ILogger<PublicBranchMenuCache> logger)
    : IPublicBranchMenuCache
{
    public async Task<PublicBranchMenuResponse?> GetAsync(
        Guid restaurantId,
        Guid branchId,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = await cache.GetAsync(
                GetKey(restaurantId, branchId), cancellationToken);
            return payload is null
                ? null
                : JsonSerializer.Deserialize<PublicBranchMenuResponse>(payload);
        }
        catch (RedisException exception)
        {
            LogCacheFailure(logger, "read", restaurantId, branchId, exception);
            return null;
        }
        catch (JsonException exception)
        {
            LogCacheFailure(logger, "deserialize", restaurantId, branchId, exception);
            return null;
        }
    }

    public async Task SetAsync(
        PublicBranchMenuResponse menu,
        CancellationToken cancellationToken)
    {
        try
        {
            await cache.SetAsync(
                GetKey(menu.RestaurantId, menu.BranchId),
                JsonSerializer.SerializeToUtf8Bytes(menu),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = options.TimeToLive
                },
                cancellationToken);
        }
        catch (RedisException exception)
        {
            LogCacheFailure(logger, "write", menu.RestaurantId, menu.BranchId, exception);
        }
    }

    public async Task RemoveAsync(
        Guid restaurantId,
        Guid branchId,
        CancellationToken cancellationToken)
    {
        try
        {
            await cache.RemoveAsync(GetKey(restaurantId, branchId), cancellationToken);
        }
        catch (RedisException exception)
        {
            LogCacheFailure(logger, "invalidate", restaurantId, branchId, exception);
        }
    }

    private static string GetKey(Guid restaurantId, Guid branchId) =>
        $"public-menu:v1:restaurants:{restaurantId:N}:branches:{branchId:N}";

    [LoggerMessage(3001, LogLevel.Warning,
        "Public Branch menu cache {Operation} failed for restaurant {RestaurantId} and branch {BranchId}.")]
    private static partial void LogCacheFailure(
        ILogger logger,
        string operation,
        Guid restaurantId,
        Guid branchId,
        Exception exception);
}
