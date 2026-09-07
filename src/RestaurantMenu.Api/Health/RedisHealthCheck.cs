using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using StackExchange.Redis;

namespace RestaurantMenu.Api.Health;

public sealed class RedisHealthCheck : IHealthCheck
{
    private const string ProbeKey = "health:redis";

    private readonly IDistributedCache _cache;

    public RedisHealthCheck(IDistributedCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);
        _cache = cache;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await _cache.GetAsync(ProbeKey, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (RedisException exception)
        {
            return HealthCheckResult.Unhealthy(
                "Redis cache is unavailable.",
                exception);
        }
    }
}
