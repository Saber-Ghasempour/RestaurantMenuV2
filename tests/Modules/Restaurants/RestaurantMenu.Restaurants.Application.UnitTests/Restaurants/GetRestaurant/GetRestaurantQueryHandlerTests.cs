using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Restaurants.GetRestaurant;
using RestaurantMenu.Restaurants.Application.Restaurants.ListRestaurants;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.Restaurants.Application.UnitTests.TestDoubles;

namespace RestaurantMenu.Restaurants.Application.UnitTests.Restaurants.GetRestaurant;

public sealed class GetRestaurantQueryHandlerTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 9, 6, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleShouldReturnRestaurantWhenItExists()
    {
        var restaurantId = RestaurantId.New();

        var expectedResponse =
            new RestaurantResponse(
                restaurantId.Value,
                "Query Test Restaurant",
                CreatedAtUtc,
                1);

        var readService =
            new RestaurantReadServiceStub(
                expectedResponse);
        var cache = new RestaurantCacheStub();

        var handler =
            new GetRestaurantQueryHandler(
                readService,
                cache);

        var query =
            new GetRestaurantQuery(
                restaurantId);

        var result =
            await handler.Handle(
                query,
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedResponse, result.Value);

        Assert.True(
            readService.RequestedRestaurantId.HasValue);

        Assert.Equal(
            restaurantId,
            readService.RequestedRestaurantId.Value);
        Assert.Equal(1, cache.SetCallCount);
        Assert.Equal(expectedResponse, cache.CachedRestaurant);
    }

    [Fact]
    public async Task HandleShouldReturnCachedRestaurantWithoutDatabaseQuery()
    {
        var restaurantId = RestaurantId.New();
        var cachedResponse = new RestaurantResponse(
            restaurantId.Value,
            "Cached Restaurant",
            CreatedAtUtc,
            4);
        var readService = new RestaurantReadServiceStub(null);
        var cache = new RestaurantCacheStub(cachedResponse);
        var handler = new GetRestaurantQueryHandler(
            readService,
            cache);

        var result = await handler.Handle(
            new GetRestaurantQuery(restaurantId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(cachedResponse, result.Value);
        Assert.Equal(1, cache.GetCallCount);
        Assert.Null(readService.RequestedRestaurantId);
        Assert.Equal(0, cache.SetCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnNotFoundWhenRestaurantDoesNotExist()
    {
        var restaurantId = RestaurantId.New();

        var readService =
            new RestaurantReadServiceStub(null);
        var cache = new RestaurantCacheStub();

        var handler =
            new GetRestaurantQueryHandler(
                readService,
                cache);

        var query =
            new GetRestaurantQuery(
                restaurantId);

        var result =
            await handler.Handle(
                query,
                CancellationToken.None);

        Assert.True(result.IsFailure);

        Assert.Equal(
            RestaurantErrors.NotFound(restaurantId),
            result.Error);
        Assert.Equal(0, cache.SetCallCount);
    }

    private sealed class RestaurantReadServiceStub(
        RestaurantResponse? response)
        : IRestaurantReadService
    {
        private readonly RestaurantResponse? _response =
            response;

        public RestaurantId? RequestedRestaurantId
        {
            get;
            private set;
        }

        public Task<RestaurantResponse?> GetByIdAsync(
            RestaurantId restaurantId,
            CancellationToken cancellationToken)
        {
            RequestedRestaurantId = restaurantId;

            return Task.FromResult(_response);
        }

        public Task<RestaurantsPage> GetPageAsync(
            string subject,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
