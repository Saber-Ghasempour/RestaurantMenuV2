using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Items;
using RestaurantMenu.Catalog.Application.Items.DeleteMenuItem;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;

namespace RestaurantMenu.Catalog.Application.UnitTests.Items.DeleteMenuItem;

public sealed class DeleteMenuItemCommandHandlerTests
{
    private static readonly DateTimeOffset DeletedAtUtc =
        new(2026, 9, 7, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleShouldSoftDeleteItem()
    {
        var menuItem = CreateMenuItem();
        var unitOfWork = new UnitOfWorkStub();
        var handler = CreateHandler(menuItem, unitOfWork);

        var result = await handler.Handle(
            CreateCommand(menuItem),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(menuItem.Id, result.Value);
        Assert.True(menuItem.IsDeleted);
        Assert.Equal(DeletedAtUtc, menuItem.DeletedAtUtc);
        Assert.Equal(2, menuItem.Version);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnNotFoundForWrongCategoryScope()
    {
        var menuItem = CreateMenuItem();
        var unitOfWork = new UnitOfWorkStub();
        var handler = CreateHandler(menuItem, unitOfWork);
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
        Assert.False(menuItem.IsDeleted);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnConflictForStaleVersion()
    {
        var menuItem = CreateMenuItem();
        var unitOfWork = new UnitOfWorkStub();
        var handler = CreateHandler(menuItem, unitOfWork);
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
        Assert.False(menuItem.IsDeleted);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnConflictForConcurrentDatabaseUpdate()
    {
        var menuItem = CreateMenuItem();
        var unitOfWork = new UnitOfWorkStub(
            new ConcurrencyException("Concurrent update."));
        var handler = CreateHandler(menuItem, unitOfWork);

        var result = await handler.Handle(
            CreateCommand(menuItem),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            MenuItemApplicationErrors.VersionConflict(menuItem.Id),
            result.Error);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    private static DeleteMenuItemCommandHandler CreateHandler(
        MenuItem menuItem,
        UnitOfWorkStub unitOfWork) =>
        new(
            new MenuItemRepositoryStub(menuItem),
            unitOfWork,
            new StubTimeProvider(DeletedAtUtc),
            new PublicMenuCacheInvalidatorStub());

    private static DeleteMenuItemCommand CreateCommand(
        MenuItem menuItem) =>
        new(
            menuItem.RestaurantId,
            menuItem.CategoryId,
            menuItem.Id,
            1);

    private static MenuItem CreateMenuItem()
    {
        var result = MenuItem.Create(
            MenuItemId.New(),
            Guid.CreateVersion7(),
            MenuCategoryId.New(),
            "Item",
            null,
            10m,
            "EUR",
            1,
            DeletedAtUtc.AddHours(-1));
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private sealed class MenuItemRepositoryStub(MenuItem menuItem)
        : IMenuItemRepository
    {
        public void Add(MenuItem item) =>
            throw new NotSupportedException();

        public Task<MenuItem?> GetByIdAsync(
            MenuItemId menuItemId,
            CancellationToken cancellationToken) =>
            Task.FromResult<MenuItem?>(
                menuItem.Id == menuItemId ? menuItem : null);
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

    private sealed class StubTimeProvider(DateTimeOffset utcNow)
        : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
