using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Items;
using RestaurantMenu.Catalog.Application.Items.UpdateMenuItem;
using RestaurantMenu.Catalog.Application.Variants;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Variants;

namespace RestaurantMenu.Catalog.Application.UnitTests.Items.UpdateMenuItem;

public sealed class UpdateMenuItemCommandHandlerTests
{
    [Fact]
    public async Task HandleShouldUpdateItemAndReturnVersion()
    {
        var menuItem = CreateMenuItem();
        var unitOfWork = new UnitOfWorkStub();
        var variant = CreateVariant(menuItem);
        var handler = new UpdateMenuItemCommandHandler(
            new MenuItemRepositoryStub(menuItem),
            new MenuItemVariantRepositoryStub(variant),
            unitOfWork,
            new PublicMenuCacheInvalidatorStub());

        var result = await handler.Handle(
            CreateCommand(menuItem),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
        Assert.Equal("Updated Item", menuItem.Name);
        Assert.Equal(25m, variant.Price.Amount);
        Assert.Equal("USD", variant.Price.Currency);
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
            new MenuItemVariantRepositoryStub(CreateVariant(menuItem)),
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
    public async Task HandleShouldVersionItemWhenOnlyDefaultPriceChanges()
    {
        var menuItem = CreateMenuItem();
        var variant = CreateVariant(menuItem);
        var handler = new UpdateMenuItemCommandHandler(
            new MenuItemRepositoryStub(menuItem),
            new MenuItemVariantRepositoryStub(variant),
            new UnitOfWorkStub(),
            new PublicMenuCacheInvalidatorStub());
        var command = new UpdateMenuItemCommand(
            menuItem.RestaurantId, menuItem.CategoryId, menuItem.Id,
            menuItem.Name, menuItem.Description, 11m, "EUR",
            menuItem.DisplayOrder, menuItem.Version);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
        Assert.Equal(11m, variant.Price.Amount);
    }

    [Fact]
    public async Task HandleShouldReturnConflictForStaleVersion()
    {
        var menuItem = CreateMenuItem();
        var unitOfWork = new UnitOfWorkStub();
        var handler = new UpdateMenuItemCommandHandler(
            new MenuItemRepositoryStub(menuItem),
            new MenuItemVariantRepositoryStub(CreateVariant(menuItem)),
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
            new MenuItemVariantRepositoryStub(CreateVariant(menuItem)),
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
    public async Task HandleShouldRejectCurrencyThatDiffersFromSiblingVariant()
    {
        var menuItem = CreateMenuItem();
        var unitOfWork = new UnitOfWorkStub();
        var handler = new UpdateMenuItemCommandHandler(
            new MenuItemRepositoryStub(menuItem),
            new MenuItemVariantRepositoryStub(
                CreateVariant(menuItem), hasDifferentCurrency: true),
            unitOfWork,
            new PublicMenuCacheInvalidatorStub());

        var result = await handler.Handle(
            CreateCommand(menuItem), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(MenuItemVariantApplicationErrors.CurrencyMismatch, result.Error);
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
            new MenuItemVariantRepositoryStub(CreateVariant(menuItem)),
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
            1,
            DateTimeOffset.UtcNow);
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static MenuItemVariant CreateVariant(MenuItem item) =>
        MenuItemVariant.Create(MenuItemVariantId.New(), item.RestaurantId,
            item.Id, "Default", null, 10m, "EUR", 0, true,
            DateTimeOffset.UtcNow).Value;

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

    private sealed class MenuItemVariantRepositoryStub(
        MenuItemVariant variant,
        bool hasDifferentCurrency = false)
        : IMenuItemVariantRepository
    {
        public void Add(MenuItemVariant value) => throw new NotSupportedException();
        public Task<MenuItemVariant?> GetByIdAsync(MenuItemVariantId id, CancellationToken cancellationToken) =>
            Task.FromResult<MenuItemVariant?>(variant.Id == id ? variant : null);
        public Task<MenuItemVariant?> GetDefaultAsync(MenuItemId id, CancellationToken cancellationToken) =>
            Task.FromResult<MenuItemVariant?>(variant.MenuItemId == id ? variant : null);
        public Task<bool> NameExistsAsync(MenuItemId id, string name, MenuItemVariantId? excluding, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> HasDifferentCurrencyAsync(MenuItemId id, string currency, MenuItemVariantId? excluding, CancellationToken cancellationToken) => Task.FromResult(hasDifferentCurrency);
        public Task<bool> SwitchDefaultAsync(MenuItemVariant currentDefault, MenuItemVariant newDefault, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
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
