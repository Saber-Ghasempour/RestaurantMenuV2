using RestaurantMenu.Catalog.Application;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Abstractions.Restaurants;
using RestaurantMenu.Catalog.Application.PublicMenus.GetPublicMenu;

namespace RestaurantMenu.Catalog.Application.UnitTests.PublicMenus.GetPublicMenu;

public sealed class GetPublicMenuQueryHandlerTests
{
    [Fact]
    public async Task HandleShouldComposeRestaurantProfileAndCatalogProjection()
    {
        var restaurantId = Guid.CreateVersion7();
        var categories = CreateCategories();
        var readService = new PublicMenuReadServiceStub(categories);
        var restaurantProvider = new RestaurantPublicProfileProviderStub(
            new RestaurantPublicProfile(
                restaurantId,
                "Public Bistro"));
        var handler = new GetPublicMenuQueryHandler(
            readService,
            restaurantProvider);

        var result = await handler.Handle(
            new GetPublicMenuQuery(restaurantId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(restaurantId, result.Value.RestaurantId);
        Assert.Equal("Public Bistro", result.Value.RestaurantName);
        Assert.Same(categories, result.Value.Categories);
        Assert.Equal(restaurantId, readService.RequestedRestaurantId);
        Assert.Equal(restaurantId, restaurantProvider.RequestedRestaurantId);
    }

    [Fact]
    public async Task HandleShouldReturnEmptyMenuForRestaurantWithoutCategories()
    {
        var restaurantId = Guid.CreateVersion7();
        var handler = new GetPublicMenuQueryHandler(
            new PublicMenuReadServiceStub([]),
            new RestaurantPublicProfileProviderStub(
                new RestaurantPublicProfile(
                    restaurantId,
                    "Empty Bistro")));

        var result = await handler.Handle(
            new GetPublicMenuQuery(restaurantId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Categories);
    }

    [Fact]
    public async Task HandleShouldReturnNotFoundWithoutQueryingCatalog()
    {
        var restaurantId = Guid.CreateVersion7();
        var readService = new PublicMenuReadServiceStub([]);
        var handler = new GetPublicMenuQueryHandler(
            readService,
            new RestaurantPublicProfileProviderStub(null));

        var result = await handler.Handle(
            new GetPublicMenuQuery(restaurantId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            CatalogApplicationErrors.RestaurantNotFound(restaurantId),
            result.Error);
        Assert.Null(readService.RequestedRestaurantId);
    }

    private static IReadOnlyList<PublicMenuCategoryResponse>
        CreateCategories() =>
        [
            new(
                Guid.CreateVersion7(),
                null,
                "Drinks",
                1,
                [
                    new(
                        Guid.CreateVersion7(),
                        "Coffee",
                        null,
                        3.50m,
                        "EUR",
                        1)
                ])
        ];

    private sealed class PublicMenuReadServiceStub(
        IReadOnlyList<PublicMenuCategoryResponse> categories)
        : IPublicMenuReadService
    {
        public Guid? RequestedRestaurantId { get; private set; }

        public Task<IReadOnlyList<PublicMenuCategoryResponse>>
            GetByRestaurantIdAsync(
                Guid restaurantId,
                CancellationToken cancellationToken)
        {
            RequestedRestaurantId = restaurantId;
            return Task.FromResult(categories);
        }
    }

    private sealed class RestaurantPublicProfileProviderStub(
        RestaurantPublicProfile? restaurant)
        : IRestaurantPublicProfileProvider
    {
        public Guid? RequestedRestaurantId { get; private set; }

        public Task<RestaurantPublicProfile?> GetAsync(
            Guid restaurantId,
            CancellationToken cancellationToken)
        {
            RequestedRestaurantId = restaurantId;
            return Task.FromResult(restaurant);
        }
    }
}
