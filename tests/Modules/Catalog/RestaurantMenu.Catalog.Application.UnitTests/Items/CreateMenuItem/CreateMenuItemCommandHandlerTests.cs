using RestaurantMenu.Catalog.Application;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Abstractions.Restaurants;
using RestaurantMenu.Catalog.Application.Items.CreateMenuItem;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;

namespace RestaurantMenu.Catalog.Application.UnitTests.Items.CreateMenuItem;

public sealed class CreateMenuItemCommandHandlerTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 9, 7, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleShouldCreateAndPersistMenuItem()
    {
        var restaurantId = Guid.CreateVersion7();
        var category = CreateCategory(restaurantId);
        var itemRepository = new MenuItemRepositorySpy();
        var unitOfWork = new UnitOfWorkSpy();
        var handler = CreateHandler(
            category,
            itemRepository,
            unitOfWork,
            restaurantExists: true);

        var result = await handler.Handle(
            new CreateMenuItemCommand(
                restaurantId,
                category.Id.Value,
                " Carbonara ",
                " Classic pasta ",
                14.50m,
                "eur",
                10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(itemRepository.AddedItem);
        Assert.Equal(result.Value, itemRepository.AddedItem.Id);
        Assert.Equal("Carbonara", itemRepository.AddedItem.Name);
        Assert.Equal(14.50m, itemRepository.AddedItem.Price.Amount);
        Assert.Equal("EUR", itemRepository.AddedItem.Price.Currency);
        Assert.Equal(UtcNow, itemRepository.AddedItem.CreatedAtUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnNotFoundForUnknownRestaurant()
    {
        var restaurantId = Guid.CreateVersion7();
        var itemRepository = new MenuItemRepositorySpy();
        var unitOfWork = new UnitOfWorkSpy();
        var handler = CreateHandler(
            null,
            itemRepository,
            unitOfWork,
            restaurantExists: false);

        var result = await handler.Handle(
            CreateCommand(restaurantId, Guid.CreateVersion7()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            CatalogApplicationErrors.RestaurantNotFound(restaurantId),
            result.Error);
        Assert.Null(itemRepository.AddedItem);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldRejectCategoryFromAnotherRestaurant()
    {
        var restaurantId = Guid.CreateVersion7();
        var category = CreateCategory(Guid.CreateVersion7());
        var itemRepository = new MenuItemRepositorySpy();
        var unitOfWork = new UnitOfWorkSpy();
        var handler = CreateHandler(
            category,
            itemRepository,
            unitOfWork,
            restaurantExists: true);

        var result = await handler.Handle(
            CreateCommand(restaurantId, category.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            CreateMenuItemApplicationErrors.CategoryNotFound(
                category.Id.Value),
            result.Error);
        Assert.Null(itemRepository.AddedItem);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldNotPersistInvalidPrice()
    {
        var restaurantId = Guid.CreateVersion7();
        var category = CreateCategory(restaurantId);
        var itemRepository = new MenuItemRepositorySpy();
        var unitOfWork = new UnitOfWorkSpy();
        var handler = CreateHandler(
            category,
            itemRepository,
            unitOfWork,
            restaurantExists: true);

        var result = await handler.Handle(
            CreateCommand(
                restaurantId,
                category.Id.Value,
                priceAmount: -1m),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(MenuItemErrors.NegativePrice, result.Error);
        Assert.Null(itemRepository.AddedItem);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private static CreateMenuItemCommandHandler CreateHandler(
        MenuCategory? category,
        MenuItemRepositorySpy itemRepository,
        UnitOfWorkSpy unitOfWork,
        bool restaurantExists) =>
        new(
            new MenuCategoryRepositoryStub(category),
            itemRepository,
            new RestaurantExistenceCheckerStub(restaurantExists),
            unitOfWork,
            new StubTimeProvider(UtcNow));

    private static CreateMenuItemCommand CreateCommand(
        Guid restaurantId,
        Guid categoryId,
        decimal priceAmount = 10m) =>
        new(
            restaurantId,
            categoryId,
            "Menu Item",
            null,
            priceAmount,
            "EUR",
            1);

    private static MenuCategory CreateCategory(Guid restaurantId)
    {
        var result = MenuCategory.Create(
            MenuCategoryId.New(),
            restaurantId,
            null,
            "Category",
            1,
            UtcNow);
        Assert.True(result.IsSuccess);
        return result.Value;
    }

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

    private sealed class MenuItemRepositorySpy : IMenuItemRepository
    {
        public MenuItem? AddedItem { get; private set; }

        public void Add(MenuItem menuItem) => AddedItem = menuItem;
    }

    private sealed class RestaurantExistenceCheckerStub(bool exists)
        : IRestaurantExistenceChecker
    {
        public Task<bool> ExistsAsync(
            Guid restaurantId,
            CancellationToken cancellationToken) =>
            Task.FromResult(exists);
    }

    private sealed class UnitOfWorkSpy : ICatalogUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class StubTimeProvider(DateTimeOffset utcNow)
        : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
