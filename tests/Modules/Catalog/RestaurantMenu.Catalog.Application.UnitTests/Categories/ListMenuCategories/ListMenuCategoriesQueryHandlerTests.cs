using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Abstractions.Restaurants;
using RestaurantMenu.Catalog.Application;
using RestaurantMenu.Catalog.Application.Categories;
using RestaurantMenu.Catalog.Application.Categories.GetMenuCategory;
using RestaurantMenu.Catalog.Application.Categories.ListMenuCategories;
using RestaurantMenu.Catalog.Domain.Categories;

namespace RestaurantMenu.Catalog.Application.UnitTests.Categories.ListMenuCategories;

public sealed class ListMenuCategoriesQueryHandlerTests
{
    [Fact]
    public async Task HandleShouldReturnCategoriesForExistingRestaurant()
    {
        var restaurantId = Guid.CreateVersion7();
        IReadOnlyList<MenuCategoryResponse> categories =
        [
            new(
                Guid.CreateVersion7(),
                restaurantId,
                null,
                "Drinks",
                1,
                DateTimeOffset.UtcNow,
                1)
        ];
        var readService = new MenuCategoryReadServiceStub(categories);
        var handler = new ListMenuCategoriesQueryHandler(
            readService,
            new RestaurantExistenceCheckerStub(true));

        var result = await handler.Handle(
            new ListMenuCategoriesQuery(restaurantId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(categories, result.Value);
        Assert.Equal(restaurantId, readService.RequestedRestaurantId);
    }

    [Fact]
    public async Task HandleShouldReturnEmptyListForRestaurantWithoutCategories()
    {
        var restaurantId = Guid.CreateVersion7();
        IReadOnlyList<MenuCategoryResponse> categories = [];
        var handler = new ListMenuCategoriesQueryHandler(
            new MenuCategoryReadServiceStub(categories),
            new RestaurantExistenceCheckerStub(true));

        var result = await handler.Handle(
            new ListMenuCategoriesQuery(restaurantId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task HandleShouldReturnNotFoundForUnknownRestaurant()
    {
        var restaurantId = Guid.CreateVersion7();
        var readService = new MenuCategoryReadServiceStub([]);
        var handler = new ListMenuCategoriesQueryHandler(
            readService,
            new RestaurantExistenceCheckerStub(false));

        var result = await handler.Handle(
            new ListMenuCategoriesQuery(restaurantId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            CatalogApplicationErrors.RestaurantNotFound(
                restaurantId),
            result.Error);
        Assert.Null(readService.RequestedRestaurantId);
    }

    private sealed class MenuCategoryReadServiceStub(
        IReadOnlyList<MenuCategoryResponse> categories)
        : IMenuCategoryReadService
    {
        public Guid? RequestedRestaurantId { get; private set; }

        public Task<MenuCategoryResponse?> GetByIdAsync(
            Guid restaurantId,
            MenuCategoryId categoryId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<MenuCategoryResponse?>(null);
        }

        public Task<IReadOnlyList<MenuCategoryResponse>>
            GetByRestaurantIdAsync(
                Guid restaurantId,
                CancellationToken cancellationToken)
        {
            RequestedRestaurantId = restaurantId;
            return Task.FromResult(categories);
        }
    }

    private sealed class RestaurantExistenceCheckerStub(bool exists)
        : IRestaurantExistenceChecker
    {
        public Task<bool> ExistsAsync(
            Guid restaurantId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(exists);
        }
    }
}
