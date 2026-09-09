using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Variants;
using RestaurantMenu.Catalog.Application.Variants.CreateMenuItemVariant;
using RestaurantMenu.Catalog.Application.Variants.DeleteMenuItemVariant;
using RestaurantMenu.Catalog.Application.Variants.SetDefaultMenuItemVariant;
using RestaurantMenu.Catalog.Application.Variants.UpdateMenuItemVariant;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Variants;

namespace RestaurantMenu.Catalog.Application.UnitTests.Variants;

public sealed class MenuItemVariantCommandHandlerTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 9, 9, 13, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateShouldAddVariantAndDemotePreviousDefault()
    {
        var item = CreateItem(published: true);
        var current = CreateVariant(item, "Default", true);
        var repository = new VariantRepositoryStub(current);
        var cache = new PublicMenuCacheInvalidatorStub();
        var handler = new CreateMenuItemVariantCommandHandler(
            new ItemRepositoryStub(item), repository, new UnitOfWorkStub(),
            cache, new StubTimeProvider());

        var result = await handler.Handle(new CreateMenuItemVariantCommand(
            item.RestaurantId, item.CategoryId, item.Id, "Large", null,
            15m, "eur", 1, true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(current.IsDefault);
        Assert.NotNull(repository.Added);
        Assert.True(repository.Added.IsDefault);
        Assert.Equal("EUR", repository.Added.Price.Currency);
        Assert.Equal(1, cache.InvalidationCount);
    }

    [Fact]
    public async Task CreateShouldRequireFirstVariantToBeDefault()
    {
        var item = CreateItem();
        var handler = new CreateMenuItemVariantCommandHandler(
            new ItemRepositoryStub(item), new VariantRepositoryStub(),
            new UnitOfWorkStub(), new PublicMenuCacheInvalidatorStub(),
            new StubTimeProvider());

        var result = await handler.Handle(new CreateMenuItemVariantCommand(
            item.RestaurantId, item.CategoryId, item.Id, "Small", null,
            8m, "EUR", 0, false), CancellationToken.None);

        Assert.Equal(MenuItemVariantApplicationErrors.DefaultRequired, result.Error);
    }

    [Fact]
    public async Task CreateShouldRejectDifferentCurrencyAndDuplicateName()
    {
        var item = CreateItem();
        var current = CreateVariant(item, "Default", true);
        var repository = new VariantRepositoryStub(current);
        var handler = new CreateMenuItemVariantCommandHandler(
            new ItemRepositoryStub(item), repository, new UnitOfWorkStub(),
            new PublicMenuCacheInvalidatorStub(), new StubTimeProvider());

        var mismatch = await handler.Handle(new CreateMenuItemVariantCommand(
            item.RestaurantId, item.CategoryId, item.Id, "Large", null,
            12m, "USD", 1, false), CancellationToken.None);
        Assert.Equal(MenuItemVariantApplicationErrors.CurrencyMismatch, mismatch.Error);

        repository.DuplicateName = true;
        var duplicate = await handler.Handle(new CreateMenuItemVariantCommand(
            item.RestaurantId, item.CategoryId, item.Id, "Default", null,
            12m, "EUR", 1, false), CancellationToken.None);
        Assert.Equal(MenuItemVariantApplicationErrors.DuplicateName, duplicate.Error);
    }

    [Fact]
    public async Task UpdateShouldRejectCrossItemVariant()
    {
        var item = CreateItem();
        var foreignItem = CreateItem();
        var variant = CreateVariant(foreignItem, "Default", true);
        var handler = new UpdateMenuItemVariantCommandHandler(
            new ItemRepositoryStub(item), new VariantRepositoryStub(variant),
            new UnitOfWorkStub(), new PublicMenuCacheInvalidatorStub());

        var result = await handler.Handle(new UpdateMenuItemVariantCommand(
            item.RestaurantId, item.CategoryId, item.Id, variant.Id,
            "Large", null, 12m, "EUR", 1, 1), CancellationToken.None);

        Assert.Equal(
            MenuItemVariantApplicationErrors.VariantNotFound(variant.Id), result.Error);
    }

    [Fact]
    public async Task SetDefaultShouldRejectStaleVersion()
    {
        var item = CreateItem();
        var variant = CreateVariant(item, "Large", false);
        var handler = new SetDefaultMenuItemVariantCommandHandler(
            new ItemRepositoryStub(item), new VariantRepositoryStub(variant),
            new PublicMenuCacheInvalidatorStub());

        var result = await handler.Handle(new SetDefaultMenuItemVariantCommand(
            item.RestaurantId, item.CategoryId, item.Id, variant.Id, 42),
            CancellationToken.None);

        Assert.Equal(
            MenuItemVariantApplicationErrors.VersionConflict(variant.Id), result.Error);
    }

    [Fact]
    public async Task DeleteShouldRejectDefaultVariant()
    {
        var item = CreateItem();
        var variant = CreateVariant(item, "Default", true);
        var handler = new DeleteMenuItemVariantCommandHandler(
            new ItemRepositoryStub(item), new VariantRepositoryStub(variant),
            new UnitOfWorkStub(), new PublicMenuCacheInvalidatorStub(),
            new StubTimeProvider());

        var result = await handler.Handle(new DeleteMenuItemVariantCommand(
            item.RestaurantId, item.CategoryId, item.Id, variant.Id, 1),
            CancellationToken.None);

        Assert.Equal(MenuItemVariantErrors.DefaultCannotBeDeleted, result.Error);
    }

    private static MenuItem CreateItem(bool published = false)
    {
        var item = MenuItem.Create(MenuItemId.New(), Guid.CreateVersion7(),
            MenuCategoryId.New(), "Item", null, 1, UtcNow).Value;
        if (published) item.ChangePublication(true);
        return item;
    }

    private static MenuItemVariant CreateVariant(
        MenuItem item, string name, bool isDefault) =>
        MenuItemVariant.Create(MenuItemVariantId.New(), item.RestaurantId,
            item.Id, name, null, 10m, "EUR", 0, isDefault, UtcNow).Value;

    private sealed class ItemRepositoryStub(MenuItem item) : IMenuItemRepository
    {
        public void Add(MenuItem value) => throw new NotSupportedException();
        public Task<MenuItem?> GetByIdAsync(MenuItemId id, CancellationToken cancellationToken) =>
            Task.FromResult<MenuItem?>(item.Id == id ? item : null);
    }

    private sealed class VariantRepositoryStub(params MenuItemVariant[] values)
        : IMenuItemVariantRepository
    {
        public bool DuplicateName { get; set; }
        public MenuItemVariant? Added { get; private set; }
        public void Add(MenuItemVariant variant) => Added = variant;
        public Task<MenuItemVariant?> GetByIdAsync(MenuItemVariantId id, CancellationToken cancellationToken) =>
            Task.FromResult(values.SingleOrDefault(value => value.Id == id));
        public Task<MenuItemVariant?> GetDefaultAsync(MenuItemId id, CancellationToken cancellationToken) =>
            Task.FromResult(values.SingleOrDefault(value => value.MenuItemId == id && value.IsDefault));
        public Task<bool> NameExistsAsync(MenuItemId id, string name, MenuItemVariantId? excluding, CancellationToken cancellationToken) =>
            Task.FromResult(DuplicateName);
        public Task<bool> HasDifferentCurrencyAsync(MenuItemId id, string currency, MenuItemVariantId? excluding, CancellationToken cancellationToken) =>
            Task.FromResult(values.Any(value => value.Id != excluding && value.Price.Currency != currency));
        public Task<bool> SwitchDefaultAsync(MenuItemVariant currentDefault, MenuItemVariant newDefault, CancellationToken cancellationToken)
        {
            currentDefault.RemoveDefault();
            newDefault.MakeDefault();
            return Task.FromResult(true);
        }
    }

    private sealed class UnitOfWorkStub : ICatalogUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(1);
    }

    private sealed class StubTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
