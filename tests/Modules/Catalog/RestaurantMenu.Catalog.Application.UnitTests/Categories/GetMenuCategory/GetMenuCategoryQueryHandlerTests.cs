using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Categories;
using RestaurantMenu.Catalog.Application.Categories.GetMenuCategory;
using RestaurantMenu.Catalog.Domain.Categories;

namespace RestaurantMenu.Catalog.Application.UnitTests.Categories.GetMenuCategory;

public sealed class GetMenuCategoryQueryHandlerTests
{
    [Fact]
    public async Task HandleShouldReturnCategoryWhenItExists()
    {
        var restaurantId = Guid.CreateVersion7();
        var categoryId = MenuCategoryId.New();
        var response = new MenuCategoryResponse(
            categoryId.Value,
            restaurantId,
            null,
            "Drinks",
            1,
            DateTimeOffset.UtcNow,
            1);
        var readService = new MenuCategoryReadServiceStub(response);
        var handler = new GetMenuCategoryQueryHandler(readService);

        var result = await handler.Handle(
            new GetMenuCategoryQuery(restaurantId, categoryId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(response, result.Value);
        Assert.Equal(restaurantId, readService.RequestedRestaurantId);
        Assert.Equal(categoryId, readService.RequestedCategoryId);
    }

    [Fact]
    public async Task HandleShouldReturnNotFoundWhenCategoryDoesNotExist()
    {
        var restaurantId = Guid.CreateVersion7();
        var categoryId = MenuCategoryId.New();
        var handler = new GetMenuCategoryQueryHandler(
            new MenuCategoryReadServiceStub(null));

        var result = await handler.Handle(
            new GetMenuCategoryQuery(restaurantId, categoryId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            MenuCategoryApplicationErrors.CategoryNotFound(categoryId),
            result.Error);
    }

    private sealed class MenuCategoryReadServiceStub(
        MenuCategoryResponse? response)
        : IMenuCategoryReadService
    {
        public Guid? RequestedRestaurantId { get; private set; }

        public MenuCategoryId? RequestedCategoryId { get; private set; }

        public Task<MenuCategoryResponse?> GetByIdAsync(
            Guid restaurantId,
            MenuCategoryId categoryId,
            CancellationToken cancellationToken)
        {
            RequestedRestaurantId = restaurantId;
            RequestedCategoryId = categoryId;
            return Task.FromResult(response);
        }

        public Task<IReadOnlyList<MenuCategoryResponse>>
            GetByRestaurantIdAsync(
                Guid restaurantId,
                CancellationToken cancellationToken)
        {
            IReadOnlyList<MenuCategoryResponse> categories = [];
            return Task.FromResult(categories);
        }
    }
}
