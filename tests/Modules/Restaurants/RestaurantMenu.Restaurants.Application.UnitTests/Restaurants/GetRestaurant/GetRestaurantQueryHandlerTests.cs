using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Restaurants.GetRestaurant;
using RestaurantMenu.Restaurants.Application.Restaurants.ListRestaurants;
using RestaurantMenu.Restaurants.Domain.Restaurants;

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

        var handler =
            new GetRestaurantQueryHandler(
                readService);

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
    }

    [Fact]
    public async Task HandleShouldReturnNotFoundWhenRestaurantDoesNotExist()
    {
        var restaurantId = RestaurantId.New();

        var readService =
            new RestaurantReadServiceStub(null);

        var handler =
            new GetRestaurantQueryHandler(
                readService);

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
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
