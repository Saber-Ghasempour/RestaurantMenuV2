using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Categories;
using RestaurantMenu.Catalog.Application.Categories.UpdateMenuCategory;
using RestaurantMenu.Catalog.Domain.Categories;

namespace RestaurantMenu.Catalog.Application.UnitTests.Categories.UpdateMenuCategory;

public sealed class UpdateMenuCategoryCommandHandlerTests
{
    [Fact]
    public async Task HandleShouldUpdateCategoryAndReturnVersion()
    {
        var restaurantId = Guid.CreateVersion7();
        var category = CreateCategory(restaurantId, null, "Original");
        var parent = CreateCategory(restaurantId, null, "Parent");
        var unitOfWork = new UnitOfWorkStub();
        var handler = CreateHandler(
            new MenuCategoryRepositoryStub(category, parent),
            unitOfWork);

        var result = await handler.Handle(
            new UpdateMenuCategoryCommand(
                restaurantId,
                category.Id,
                parent.Id.Value,
                " Updated ",
                20,
                1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
        Assert.Equal(parent.Id, category.ParentId);
        Assert.Equal("Updated", category.Name);
        Assert.Equal(20, category.DisplayOrder);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnNotFoundForAnotherRestaurantsCategory()
    {
        var category = CreateCategory(
            Guid.CreateVersion7(),
            null,
            "Category");
        var unitOfWork = new UnitOfWorkStub();
        var handler = CreateHandler(
            new MenuCategoryRepositoryStub(category),
            unitOfWork);

        var result = await handler.Handle(
            new UpdateMenuCategoryCommand(
                Guid.CreateVersion7(),
                category.Id,
                null,
                "Updated",
                1,
                1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            MenuCategoryApplicationErrors.CategoryNotFound(category.Id),
            result.Error);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnConflictForStaleVersion()
    {
        var restaurantId = Guid.CreateVersion7();
        var category = CreateCategory(restaurantId, null, "Category");
        var unitOfWork = new UnitOfWorkStub();
        var handler = CreateHandler(
            new MenuCategoryRepositoryStub(category),
            unitOfWork);

        var result = await handler.Handle(
            new UpdateMenuCategoryCommand(
                restaurantId,
                category.Id,
                null,
                "Updated",
                1,
                42),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            MenuCategoryApplicationErrors.VersionConflict(category.Id),
            result.Error);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldRejectParentFromAnotherRestaurant()
    {
        var restaurantId = Guid.CreateVersion7();
        var category = CreateCategory(restaurantId, null, "Category");
        var parent = CreateCategory(
            Guid.CreateVersion7(),
            null,
            "Other Parent");
        var unitOfWork = new UnitOfWorkStub();
        var handler = CreateHandler(
            new MenuCategoryRepositoryStub(category, parent),
            unitOfWork);

        var result = await handler.Handle(
            new UpdateMenuCategoryCommand(
                restaurantId,
                category.Id,
                parent.Id.Value,
                "Updated",
                1,
                1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            MenuCategoryApplicationErrors.ParentCategoryNotFound(
                parent.Id.Value),
            result.Error);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldRejectMovingCategoryBelowItsDescendant()
    {
        var restaurantId = Guid.CreateVersion7();
        var category = CreateCategory(restaurantId, null, "Parent");
        var child = CreateCategory(
            restaurantId,
            category.Id,
            "Child");
        var unitOfWork = new UnitOfWorkStub();
        var handler = CreateHandler(
            new MenuCategoryRepositoryStub(category, child),
            unitOfWork);

        var result = await handler.Handle(
            new UpdateMenuCategoryCommand(
                restaurantId,
                category.Id,
                child.Id.Value,
                "Parent",
                1,
                1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            MenuCategoryApplicationErrors.HierarchyCycle(category.Id),
            result.Error);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnConflictForConcurrentDatabaseUpdate()
    {
        var restaurantId = Guid.CreateVersion7();
        var category = CreateCategory(restaurantId, null, "Category");
        var unitOfWork = new UnitOfWorkStub(
            new ConcurrencyException("Concurrent update."));
        var handler = CreateHandler(
            new MenuCategoryRepositoryStub(category),
            unitOfWork);

        var result = await handler.Handle(
            new UpdateMenuCategoryCommand(
                restaurantId,
                category.Id,
                null,
                "Updated",
                1,
                1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            MenuCategoryApplicationErrors.VersionConflict(category.Id),
            result.Error);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    private static UpdateMenuCategoryCommandHandler CreateHandler(
        MenuCategoryRepositoryStub repository,
        UnitOfWorkStub unitOfWork) =>
        new(repository, unitOfWork);

    private static MenuCategory CreateCategory(
        Guid restaurantId,
        MenuCategoryId? parentId,
        string name)
    {
        var result = MenuCategory.Create(
            MenuCategoryId.New(),
            restaurantId,
            parentId,
            name,
            1,
            DateTimeOffset.UtcNow);
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private sealed class MenuCategoryRepositoryStub(
        params MenuCategory[] categories)
        : IMenuCategoryRepository
    {
        public void Add(MenuCategory category) =>
            throw new NotSupportedException();

        public Task<MenuCategory?> GetByIdAsync(
            MenuCategoryId categoryId,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                categories.SingleOrDefault(
                    category => category.Id == categoryId));

        public Task<bool> HasChildrenAsync(
            MenuCategoryId categoryId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);
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
