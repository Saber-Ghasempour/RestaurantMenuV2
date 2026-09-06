using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Items;
using RestaurantMenu.Catalog.Application.Items.GetMenuItem;
using RestaurantMenu.Catalog.Application.Items.ListMenuItems;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;

namespace RestaurantMenu.Catalog.Application.UnitTests.Items.ListMenuItems;

public sealed class ListMenuItemsQueryHandlerTests
{
    [Fact]
    public async Task HandleShouldReturnItemsForOwnedCategory()
    {
        var restaurantId = Guid.CreateVersion7();
        var category = CreateCategory(restaurantId);
        IReadOnlyList<MenuItemResponse> items =
        [
            CreateResponse(restaurantId, category.Id)
        ];
        var readService = new MenuItemReadServiceStub(items);
        var handler = new ListMenuItemsQueryHandler(
            new MenuCategoryRepositoryStub(category),
            readService);

        var result = await handler.Handle(
            new ListMenuItemsQuery(restaurantId, category.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(items, result.Value);
        Assert.Equal(restaurantId, readService.RestaurantId);
        Assert.Equal(category.Id, readService.CategoryId);
    }

    [Fact]
    public async Task HandleShouldReturnEmptyListForCategoryWithoutItems()
    {
        var restaurantId = Guid.CreateVersion7();
        var category = CreateCategory(restaurantId);
        var handler = new ListMenuItemsQueryHandler(
            new MenuCategoryRepositoryStub(category),
            new MenuItemReadServiceStub([]));

        var result = await handler.Handle(
            new ListMenuItemsQuery(restaurantId, category.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task HandleShouldReturnNotFoundForAnotherRestaurantsCategory()
    {
        var category = CreateCategory(Guid.CreateVersion7());
        var readService = new MenuItemReadServiceStub([]);
        var handler = new ListMenuItemsQueryHandler(
            new MenuCategoryRepositoryStub(category),
            readService);

        var result = await handler.Handle(
            new ListMenuItemsQuery(
                Guid.CreateVersion7(),
                category.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            MenuItemApplicationErrors.CategoryNotFound(
                category.Id.Value),
            result.Error);
        Assert.Null(readService.RestaurantId);
    }

    private static MenuCategory CreateCategory(Guid restaurantId)
    {
        var result = MenuCategory.Create(
            MenuCategoryId.New(),
            restaurantId,
            null,
            "Category",
            1,
            DateTimeOffset.UtcNow);
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static MenuItemResponse CreateResponse(
        Guid restaurantId,
        MenuCategoryId categoryId) =>
        new(
            Guid.CreateVersion7(),
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

    private sealed class MenuCategoryRepositoryStub(
        MenuCategory? category)
        : IMenuCategoryRepository
    {
        public void Add(MenuCategory categoryToAdd) =>
            throw new NotSupportedException();

        public Task<MenuCategory?> GetByIdAsync(
            MenuCategoryId categoryId,
            CancellationToken cancellationToken) =>
            Task.FromResult(category);

        public Task<bool> HasChildrenAsync(
            MenuCategoryId categoryId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }

    private sealed class MenuItemReadServiceStub(
        IReadOnlyList<MenuItemResponse> items)
        : IMenuItemReadService
    {
        public Guid? RestaurantId { get; private set; }

        public MenuCategoryId? CategoryId { get; private set; }

        public Task<MenuItemResponse?> GetByIdAsync(
            Guid restaurantId,
            MenuCategoryId categoryId,
            MenuItemId menuItemId,
            CancellationToken cancellationToken) =>
            Task.FromResult<MenuItemResponse?>(null);

        public Task<IReadOnlyList<MenuItemResponse>>
            GetByCategoryIdAsync(
                Guid restaurantId,
                MenuCategoryId categoryId,
                CancellationToken cancellationToken)
        {
            RestaurantId = restaurantId;
            CategoryId = categoryId;
            return Task.FromResult(items);
        }
    }
}
