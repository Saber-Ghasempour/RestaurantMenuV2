using RestaurantMenu.Catalog.Application.Abstractions.Caching;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Categories.ChangeMenuCategoryPublication;
using RestaurantMenu.Catalog.Application.Categories.UpdateMenuCategoryContent;
using RestaurantMenu.Catalog.Application.Items.ChangeMenuItemPublication;
using RestaurantMenu.Catalog.Application.Items.ChangeMenuItemAvailability;
using RestaurantMenu.Catalog.Application.Items.UpdateMenuItemMetadata;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;

namespace RestaurantMenu.Catalog.Application.UnitTests.Items;

public sealed class CatalogMetadataCommandHandlerTests
{
    [Fact]
    public async Task CategoryContentShouldCommitThenInvalidatePublicMenus()
    {
        var category = CreateCategory();
        var unitOfWork = new UnitOfWorkStub();
        var invalidator = new InvalidatorStub();
        var handler = new UpdateMenuCategoryContentCommandHandler(
            new CategoryRepositoryStub(category), unitOfWork, invalidator);

        var result = await handler.Handle(new UpdateMenuCategoryContentCommand(
            category.RestaurantId, category.Id, "Seasonal", 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.CallCount);
        Assert.Equal(1, invalidator.CallCount);
    }

    [Fact]
    public async Task CategoryPublicationShouldNotInvalidateForNoOp()
    {
        var category = CreateCategory();
        var unitOfWork = new UnitOfWorkStub();
        var invalidator = new InvalidatorStub();
        var handler = new ChangeMenuCategoryPublicationCommandHandler(
            new CategoryRepositoryStub(category), unitOfWork, invalidator);

        var result = await handler.Handle(new ChangeMenuCategoryPublicationCommand(
            category.RestaurantId, category.Id, false, 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.CallCount);
        Assert.Equal(0, invalidator.CallCount);
    }

    [Fact]
    public async Task CategoryPublicationShouldRequirePublishedParent()
    {
        var parent = CreateCategory();
        var child = MenuCategory.Create(
            MenuCategoryId.New(), parent.RestaurantId, parent.Id, "Child", 2,
            DateTimeOffset.UtcNow).Value;
        var unitOfWork = new UnitOfWorkStub();
        var handler = new ChangeMenuCategoryPublicationCommandHandler(
            new CategoryRepositoryStub(child), unitOfWork, new InvalidatorStub());

        var result = await handler.Handle(new ChangeMenuCategoryPublicationCommand(
            child.RestaurantId, child.Id, true, 1), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Catalog.CategoryPublicationRequiresPublishedParent", result.Error.Code);
        Assert.Equal(0, unitOfWork.CallCount);
    }

    [Fact]
    public async Task CategoryPublicationShouldProtectPublishedChildren()
    {
        var parent = CreateCategory();
        parent.ChangePublication(true);
        var unitOfWork = new UnitOfWorkStub();
        var handler = new ChangeMenuCategoryPublicationCommandHandler(
            new CategoryRepositoryStub(parent, hasPublishedChildren: true),
            unitOfWork, new InvalidatorStub());

        var result = await handler.Handle(new ChangeMenuCategoryPublicationCommand(
            parent.RestaurantId, parent.Id, false, parent.Version),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Catalog.CategoryHasPublishedChildren", result.Error.Code);
        Assert.Equal(0, unitOfWork.CallCount);
    }

    [Fact]
    public async Task ItemMetadataShouldRejectWrongCategoryWithoutWriting()
    {
        var item = CreateItem();
        var unitOfWork = new UnitOfWorkStub();
        var invalidator = new InvalidatorStub();
        var handler = new UpdateMenuItemMetadataCommandHandler(
            new ItemRepositoryStub(item), unitOfWork, invalidator);

        var result = await handler.Handle(new UpdateMenuItemMetadataCommand(
            item.RestaurantId, MenuCategoryId.New(), item.Id, null, null, [], null,
            null, false, 1), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.CallCount);
        Assert.Equal(0, invalidator.CallCount);
    }

    [Fact]
    public async Task ItemPublicationShouldCommitThenInvalidatePublicMenus()
    {
        var item = CreateItem();
        var unitOfWork = new UnitOfWorkStub();
        var invalidator = new InvalidatorStub();
        var handler = new ChangeMenuItemPublicationCommandHandler(
            new ItemRepositoryStub(item), unitOfWork, invalidator);

        var result = await handler.Handle(new ChangeMenuItemPublicationCommand(
            item.RestaurantId, item.CategoryId, item.Id, true, 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(item.IsPublished);
        Assert.Equal(1, unitOfWork.CallCount);
        Assert.Equal(1, invalidator.CallCount);
    }

    [Fact]
    public async Task PublishedItemAvailabilityShouldInvalidatePublicMenusAfterCommit()
    {
        var item = CreateItem();
        item.ChangePublication(true);
        item.ClearDomainEvents();
        var unitOfWork = new UnitOfWorkStub();
        var invalidator = new InvalidatorStub();
        var handler = new ChangeMenuItemAvailabilityCommandHandler(
            new ItemRepositoryStub(item), unitOfWork, invalidator);

        var result = await handler.Handle(new ChangeMenuItemAvailabilityCommand(
            item.RestaurantId, item.CategoryId, item.Id, false, item.Version),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.CallCount);
        Assert.Equal(1, invalidator.CallCount);
    }

    private static MenuCategory CreateCategory() => MenuCategory.Create(
        MenuCategoryId.New(), Guid.CreateVersion7(), null, "Food", 1,
        DateTimeOffset.UtcNow).Value;

    private static MenuItem CreateItem() => MenuItem.Create(
        MenuItemId.New(), Guid.CreateVersion7(), MenuCategoryId.New(), "Soup",
        null, 1, DateTimeOffset.UtcNow).Value;

    private sealed class CategoryRepositoryStub(
        MenuCategory category,
        bool hasPublishedChildren = false)
        : IMenuCategoryRepository
    {
        public void Add(MenuCategory categoryToAdd) => throw new NotSupportedException();
        public Task<bool> HasChildrenAsync(MenuCategoryId categoryId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<bool> HasPublishedChildrenAsync(
            MenuCategoryId categoryId,
            CancellationToken cancellationToken) =>
            Task.FromResult(hasPublishedChildren);
        public Task<MenuCategory?> GetByIdAsync(MenuCategoryId categoryId, CancellationToken cancellationToken) =>
            Task.FromResult<MenuCategory?>(category);
    }

    private sealed class ItemRepositoryStub(MenuItem item) : IMenuItemRepository
    {
        public void Add(MenuItem itemToAdd) => throw new NotSupportedException();
        public Task<MenuItem?> GetByIdAsync(MenuItemId menuItemId, CancellationToken cancellationToken) =>
            Task.FromResult<MenuItem?>(item);
    }

    private sealed class UnitOfWorkStub : ICatalogUnitOfWork
    {
        public int CallCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class InvalidatorStub : IPublicMenuCacheInvalidator
    {
        public int CallCount { get; private set; }
        public Task InvalidateRestaurantAsync(Guid restaurantId, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }
}
