using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Restaurants.GetRestaurant;
using RestaurantMenu.Restaurants.Application.Restaurants.ListRestaurants;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Application.UnitTests.Restaurants.ListRestaurants;

public sealed class ListRestaurantsQueryHandlerTests
{
    private static readonly RestaurantsPage Page =
        new(
            [
                new RestaurantResponse(
                    Guid.CreateVersion7(),
                    "Paged Restaurant",
                    new DateTimeOffset(
                        2026,
                        9,
                        6,
                        19,
                        0,
                        0,
                        TimeSpan.Zero))
            ],
            2,
            10,
            21);

    [Fact]
    public async Task HandleShouldReturnRequestedPage()
    {
        var readService =
            new RestaurantReadServiceStub(Page);

        var handler =
            new ListRestaurantsQueryHandler(readService);

        var result =
            await handler.Handle(
                new ListRestaurantsQuery(2, 10),
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(Page, result.Value);
        Assert.Equal(2, readService.RequestedPage);
        Assert.Equal(10, readService.RequestedPageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task HandleShouldRejectInvalidPage(
        int page)
    {
        var readService =
            new RestaurantReadServiceStub(Page);

        var handler =
            new ListRestaurantsQueryHandler(readService);

        var result =
            await handler.Handle(
                new ListRestaurantsQuery(page, 10),
                CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ListRestaurantsErrors.InvalidPage,
            result.Error);
        Assert.Null(readService.RequestedPage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task HandleShouldRejectInvalidPageSize(
        int pageSize)
    {
        var readService =
            new RestaurantReadServiceStub(Page);

        var handler =
            new ListRestaurantsQueryHandler(readService);

        var result =
            await handler.Handle(
                new ListRestaurantsQuery(1, pageSize),
                CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ListRestaurantsErrors.InvalidPageSize,
            result.Error);
        Assert.Null(readService.RequestedPageSize);
    }

    private sealed class RestaurantReadServiceStub(
        RestaurantsPage response)
        : IRestaurantReadService
    {
        public int? RequestedPage { get; private set; }

        public int? RequestedPageSize { get; private set; }

        public Task<RestaurantResponse?> GetByIdAsync(
            RestaurantId restaurantId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<RestaurantResponse?>(null);
        }

        public Task<RestaurantsPage> GetPageAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            RequestedPage = page;
            RequestedPageSize = pageSize;

            return Task.FromResult(response);
        }
    }
}
