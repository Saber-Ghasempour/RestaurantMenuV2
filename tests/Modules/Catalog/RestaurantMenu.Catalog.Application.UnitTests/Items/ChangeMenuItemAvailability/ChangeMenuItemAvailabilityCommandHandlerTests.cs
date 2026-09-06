using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Items;
using RestaurantMenu.Catalog.Application.Items.ChangeMenuItemAvailability;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;

namespace RestaurantMenu.Catalog.Application.UnitTests.Items.ChangeMenuItemAvailability;

public sealed class ChangeMenuItemAvailabilityCommandHandlerTests
{
    [Fact]
    public async Task HandleShouldChangeAvailabilityAndReturnVersion()
    {
        var menuItem = CreateMenuItem();
        var unitOfWork = new UnitOfWorkStub();
        var handler = CreateHandler(menuItem, unitOfWork);

        var result = await handler.Handle(
            CreateCommand(menuItem, false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
        Assert.False(menuItem.IsAvailable);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldKeepVersionForNoOp()
    {
        var menuItem = CreateMenuItem();
        var unitOfWork = new UnitOfWorkStub();
        var handler = CreateHandler(menuItem, unitOfWork);

        var result = await handler.Handle(
            CreateCommand(menuItem, true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value);
        Assert.True(menuItem.IsAvailable);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnNotFoundForWrongRestaurantScope()
    {
        var menuItem = CreateMenuItem();
        var unitOfWork = new UnitOfWorkStub();
        var handler = CreateHandler(menuItem, unitOfWork);
        var command = CreateCommand(menuItem, false) with
        {
            RestaurantId = Guid.CreateVersion7()
        };

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            MenuItemApplicationErrors.ItemNotFound(menuItem.Id),
            result.Error);
        Assert.True(menuItem.IsAvailable);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnConflictForStaleVersion()
    {
        var menuItem = CreateMenuItem();
        var unitOfWork = new UnitOfWorkStub();
        var handler = CreateHandler(menuItem, unitOfWork);
        var command = CreateCommand(menuItem, false) with
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
        Assert.True(menuItem.IsAvailable);
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
            CreateCommand(menuItem, false),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            MenuItemApplicationErrors.VersionConflict(menuItem.Id),
            result.Error);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    private static ChangeMenuItemAvailabilityCommandHandler CreateHandler(
        MenuItem menuItem,
        UnitOfWorkStub unitOfWork) =>
        new(new MenuItemRepositoryStub(menuItem), unitOfWork);

    private static ChangeMenuItemAvailabilityCommand CreateCommand(
        MenuItem menuItem,
        bool isAvailable) =>
        new(
            menuItem.RestaurantId,
            menuItem.CategoryId,
            menuItem.Id,
            isAvailable,
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
            DateTimeOffset.UtcNow);
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
}
