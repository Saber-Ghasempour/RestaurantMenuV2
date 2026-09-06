using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Items;
using RestaurantMenu.Catalog.Application.Items.GetMenuItem;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;

namespace RestaurantMenu.Catalog.Application.UnitTests.Items.GetMenuItem;

public sealed class GetMenuItemQueryHandlerTests
{
    [Fact]
    public async Task HandleShouldReturnMenuItemWhenItExists()
    {
        var restaurantId = Guid.CreateVersion7();
        var categoryId = MenuCategoryId.New();
        var menuItemId = MenuItemId.New();
        var response = CreateResponse(
            restaurantId,
            categoryId,
            menuItemId);
        var readService = new MenuItemReadServiceStub(response);
        var handler = new GetMenuItemQueryHandler(readService);

        var result = await handler.Handle(
            new GetMenuItemQuery(
                restaurantId,
                categoryId,
                menuItemId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(response, result.Value);
        Assert.Equal(restaurantId, readService.RestaurantId);
        Assert.Equal(categoryId, readService.CategoryId);
        Assert.Equal(menuItemId, readService.MenuItemId);
    }

    [Fact]
    public async Task HandleShouldReturnNotFoundWhenMenuItemDoesNotExist()
    {
        var menuItemId = MenuItemId.New();
        var handler = new GetMenuItemQueryHandler(
            new MenuItemReadServiceStub(null));

        var result = await handler.Handle(
            new GetMenuItemQuery(
                Guid.CreateVersion7(),
                MenuCategoryId.New(),
                menuItemId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            MenuItemApplicationErrors.ItemNotFound(menuItemId),
            result.Error);
    }

    private static MenuItemResponse CreateResponse(
        Guid restaurantId,
        MenuCategoryId categoryId,
        MenuItemId menuItemId) =>
        new(
            menuItemId.Value,
            restaurantId,
            categoryId.Value,
            "Menu Item",
            null,
            10m,
            "EUR",
            1,
            true,
            DateTimeOffset.UtcNow,
            1);

    private sealed class MenuItemReadServiceStub(
        MenuItemResponse? response)
        : IMenuItemReadService
    {
        public Guid? RestaurantId { get; private set; }

        public MenuCategoryId? CategoryId { get; private set; }

        public MenuItemId? MenuItemId { get; private set; }

        public Task<MenuItemResponse?> GetByIdAsync(
            Guid restaurantId,
            MenuCategoryId categoryId,
            MenuItemId menuItemId,
            CancellationToken cancellationToken)
        {
            RestaurantId = restaurantId;
            CategoryId = categoryId;
            MenuItemId = menuItemId;
            return Task.FromResult(response);
        }

        public Task<IReadOnlyList<MenuItemResponse>>
            GetByCategoryIdAsync(
                Guid restaurantId,
                MenuCategoryId categoryId,
                CancellationToken cancellationToken)
        {
            IReadOnlyList<MenuItemResponse> items = [];
            return Task.FromResult(items);
        }
    }
}
