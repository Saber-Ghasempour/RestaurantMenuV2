using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using RestaurantMenu.Restaurants.Application.Restaurants.GetRestaurant;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.Restaurants.Infrastructure.Caching;

using StackExchange.Redis;

using Testcontainers.Redis;

namespace RestaurantMenu.Restaurants.IntegrationTests.Caching;

public sealed class RestaurantCacheTests : IAsyncLifetime
{
    private const string CacheInstanceName = "restaurant-menu:v1:";

    private readonly RedisContainer _redis =
        new RedisBuilder("redis:8.10.1-alpine")
            .Build();

    private ServiceProvider? _services;

    public async Task InitializeAsync()
    {
        await _redis.StartAsync();

        var services = new ServiceCollection();
        services.AddStackExchangeRedisCache(
            options =>
            {
                options.Configuration = _redis.GetConnectionString();
                options.InstanceName = CacheInstanceName;
            });

        _services = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }

        await _redis.DisposeAsync();
    }

    [Fact]
    public async Task SetShouldRoundTripResponseWithAbsoluteExpiration()
    {
        var timeToLive = TimeSpan.FromMinutes(5);
        var cache = CreateCache(timeToLive);
        var response = CreateResponse();

        await cache.SetAsync(response, CancellationToken.None);
        var cached = await cache.GetAsync(
            new RestaurantId(response.Id),
            CancellationToken.None);

        Assert.Equal(response, cached);

        using var connection = await ConnectionMultiplexer.ConnectAsync(
            _redis.GetConnectionString());
        var key =
            $"{CacheInstanceName}restaurants:{response.Id:N}";
        var remaining = await connection.GetDatabase()
            .KeyTimeToLiveAsync(key);

        Assert.NotNull(remaining);
        Assert.InRange(
            remaining.Value,
            TimeSpan.FromMinutes(4),
            timeToLive);
    }

    [Fact]
    public async Task RemoveShouldDeleteCachedResponse()
    {
        var cache = CreateCache(TimeSpan.FromMinutes(5));
        var response = CreateResponse();
        var restaurantId = new RestaurantId(response.Id);
        await cache.SetAsync(response, CancellationToken.None);

        await cache.RemoveAsync(
            restaurantId,
            CancellationToken.None);
        var cached = await cache.GetAsync(
            restaurantId,
            CancellationToken.None);

        Assert.Null(cached);
    }

    [Fact]
    public async Task GetShouldTreatInvalidJsonAsCacheMiss()
    {
        var cache = CreateCache(TimeSpan.FromMinutes(5));
        var response = CreateResponse();
        using var connection = await ConnectionMultiplexer.ConnectAsync(
            _redis.GetConnectionString());
        var key =
            $"{CacheInstanceName}restaurants:{response.Id:N}";
        await connection.GetDatabase().StringSetAsync(
            key,
            "not-json");

        var cached = await cache.GetAsync(
            new RestaurantId(response.Id),
            CancellationToken.None);

        Assert.Null(cached);
    }

    [Fact]
    public async Task RedisFailureShouldDegradeToCacheMissAndBestEffortWrites()
    {
        var cache = new RestaurantCache(
            new FaultingDistributedCache(),
            new RestaurantCacheOptions(TimeSpan.FromMinutes(5)),
            NullLogger<RestaurantCache>.Instance);
        var response = CreateResponse();
        var restaurantId = new RestaurantId(response.Id);

        var cached = await cache.GetAsync(
            restaurantId,
            CancellationToken.None);
        await cache.SetAsync(response, CancellationToken.None);
        await cache.RemoveAsync(
            restaurantId,
            CancellationToken.None);

        Assert.Null(cached);
    }

    private RestaurantCache CreateCache(TimeSpan timeToLive)
    {
        Assert.NotNull(_services);

        return new RestaurantCache(
            _services.GetRequiredService<IDistributedCache>(),
            new RestaurantCacheOptions(timeToLive),
            NullLogger<RestaurantCache>.Instance);
    }

    private static RestaurantResponse CreateResponse() =>
        new(
            Guid.CreateVersion7(),
            "Cached Integration Restaurant",
            new DateTimeOffset(
                2026,
                9,
                7,
                12,
                0,
                0,
            TimeSpan.Zero),
            3);

    private sealed class FaultingDistributedCache : IDistributedCache
    {
        public byte[]? Get(string key) => throw CreateException();

        public Task<byte[]?> GetAsync(
            string key,
            CancellationToken token = default) =>
            Task.FromException<byte[]?>(CreateException());

        public void Refresh(string key) => throw CreateException();

        public Task RefreshAsync(
            string key,
            CancellationToken token = default) =>
            Task.FromException(CreateException());

        public void Remove(string key) => throw CreateException();

        public Task RemoveAsync(
            string key,
            CancellationToken token = default) =>
            Task.FromException(CreateException());

        public void Set(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options) =>
            throw CreateException();

        public Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default) =>
            Task.FromException(CreateException());

        private static RedisConnectionException CreateException() =>
            new(
                ConnectionFailureType.UnableToConnect,
                CommandFlags.None,
                "Redis is unavailable for this test.",
                new InvalidOperationException(
                    "Synthetic connection failure."),
                default);
    }
}
