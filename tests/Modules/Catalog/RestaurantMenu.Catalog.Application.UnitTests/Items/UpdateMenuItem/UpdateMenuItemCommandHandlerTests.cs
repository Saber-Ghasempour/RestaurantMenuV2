using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Items;
using RestaurantMenu.Catalog.Application.Items.UpdateMenuItem;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;

namespace RestaurantMenu.Catalog.Application.UnitTests.Items.UpdateMenuItem;

public sealed class UpdateMenuItemCommandHandlerTests
{
    [Fact]
    public async Task HandleShouldUpdateItemAndReturnVersion()
    {
        var menuItem = CreateMenuItem();
        var unitOfWork = new UnitOfWorkStub();
        var handler = new UpdateMenuItemCommandHandler(
            new MenuItemRepositoryStub(menuItem),
            unitOfWork,
            new PublicMenuCacheInvalidatorStub());

        var result = await handler.Handle(
            CreateCommand(menuItem),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
        Assert.Equal("Updated Item", menuItem.Name);
        Assert.Equal(25m, menuItem.Price.Amount);
        Assert.Equal("USD", menuItem.Price.Currency);
        Assert.Equal(20, menuItem.DisplayOrder);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnNotFoundForWrongCategoryScope()
    {
        var menuItem = CreateMenuItem();
        var unitOfWork = new UnitOfWorkStub();
        var handler = new UpdateMenuItemCommandHandler(
            new MenuItemRepositoryStub(menuItem),
            unitOfWork,
            new PublicMenuCacheInvalidatorStub());
        var command = CreateCommand(menuItem) with
        {
            CategoryId = MenuCategoryId.New()
        };

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            MenuItemApplicationErrors.ItemNotFound(menuItem.Id),
            result.Error);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnConflictForStaleVersion()
    {
        var menuItem = CreateMenuItem();
        var unitOfWork = new UnitOfWorkStub();
        var handler = new UpdateMenuItemCommandHandler(
            new MenuItemRepositoryStub(menuItem),
            unitOfWork,
            new PublicMenuCacheInvalidatorStub());
        var command = CreateCommand(menuItem) with
        {
            ExpectedVersion = 42
        };

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            MenuItemApplicationErrors.VersionConflict(menuItem.Id),
            result.Error);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnDomainValidationFailure()
    {
        var menuItem = CreateMenuItem();
        var unitOfWork = new UnitOfWorkStub();
        var handler = new UpdateMenuItemCommandHandler(
            new MenuItemRepositoryStub(menuItem),
            unitOfWork,
            new PublicMenuCacheInvalidatorStub());
        var command = CreateCommand(menuItem) with
        {
            PriceAmount = -1m
        };

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(MenuItemErrors.NegativePrice, result.Error);
        Assert.Equal(1, menuItem.Version);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnConflictForConcurrentDatabaseUpdate()
    {
        var menuItem = CreateMenuItem();
        var unitOfWork = new UnitOfWorkStub(
            new ConcurrencyException("Concurrent update."));
        var handler = new UpdateMenuItemCommandHandler(
            new MenuItemRepositoryStub(menuItem),
            unitOfWork,
            new PublicMenuCacheInvalidatorStub());

        var result = await handler.Handle(
            CreateCommand(menuItem),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            MenuItemApplicationErrors.VersionConflict(menuItem.Id),
            result.Error);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    private static UpdateMenuItemCommand CreateCommand(
        MenuItem menuItem) =>
        new(
            menuItem.RestaurantId,
            menuItem.CategoryId,
            menuItem.Id,
            " Updated Item ",
            " Updated description ",
            25m,
            "usd",
            20,
            1);

    private static MenuItem CreateMenuItem()
    {
        var result = MenuItem.Create(
            MenuItemId.New(),
            Guid.CreateVersion7(),
            MenuCategoryId.New(),
            "Original Item",
            null,
            10m,
            "EUR",
            1,
            DateTimeOffset.UtcNow);
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private sealed class MenuItemRepositoryStub(MenuItem? menuItem)
        : IMenuItemRepository
    {
        public void Add(MenuItem item) =>
            throw new NotSupportedException();

        public Task<MenuItem?> GetByIdAsync(
            MenuItemId menuItemId,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                menuItem?.Id == menuItemId ? menuItem : null);
    }

    private sealed class UnitOfWorkStub(Exception? exception = null)
        : ICatalogUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return exception is null
                ? Task.FromResult(1)
                : Task.FromException<int>(exception);
        }
    }
}
