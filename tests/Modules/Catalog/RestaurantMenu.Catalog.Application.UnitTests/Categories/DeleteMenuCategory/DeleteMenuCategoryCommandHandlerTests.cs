using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Categories;
using RestaurantMenu.Catalog.Application.Categories.DeleteMenuCategory;
using RestaurantMenu.Catalog.Domain.Categories;

namespace RestaurantMenu.Catalog.Application.UnitTests.Categories.DeleteMenuCategory;

public sealed class DeleteMenuCategoryCommandHandlerTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleShouldSoftDeleteCategory()
    {
        var category = CreateCategory();
        var unitOfWork = new UnitOfWorkStub();
        var handler = CreateHandler(category, unitOfWork);

        var result = await handler.Handle(
            new DeleteMenuCategoryCommand(
                category.RestaurantId,
                category.Id,
                1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(category.Id, result.Value);
        Assert.True(category.IsDeleted);
        Assert.Equal(UtcNow, category.DeletedAtUtc);
        Assert.Equal(2, category.Version);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnNotFoundForAnotherRestaurantsCategory()
    {
        var category = CreateCategory();
        var unitOfWork = new UnitOfWorkStub();
        var handler = CreateHandler(category, unitOfWork);

        var result = await handler.Handle(
            new DeleteMenuCategoryCommand(
                Guid.CreateVersion7(),
                category.Id,
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
        var category = CreateCategory();
        var unitOfWork = new UnitOfWorkStub();
        var handler = CreateHandler(category, unitOfWork);

        var result = await handler.Handle(
            new DeleteMenuCategoryCommand(
                category.RestaurantId,
                category.Id,
                42),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            MenuCategoryApplicationErrors.VersionConflict(category.Id),
            result.Error);
        Assert.False(category.IsDeleted);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnConflictWhenCategoryHasChildren()
    {
        var category = CreateCategory();
        var unitOfWork = new UnitOfWorkStub();
        var handler = CreateHandler(
            category,
            unitOfWork,
            hasChildren: true);

        var result = await handler.Handle(
            new DeleteMenuCategoryCommand(
                category.RestaurantId,
                category.Id,
                1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            MenuCategoryApplicationErrors.HasChildren(category.Id),
            result.Error);
        Assert.False(category.IsDeleted);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnConflictForConcurrentDatabaseUpdate()
    {
        var category = CreateCategory();
        var unitOfWork = new UnitOfWorkStub(
            new ConcurrencyException("Concurrent update."));
        var handler = CreateHandler(category, unitOfWork);

        var result = await handler.Handle(
            new DeleteMenuCategoryCommand(
                category.RestaurantId,
                category.Id,
                1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            MenuCategoryApplicationErrors.VersionConflict(category.Id),
            result.Error);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    private static DeleteMenuCategoryCommandHandler CreateHandler(
        MenuCategory category,
        UnitOfWorkStub unitOfWork,
        bool hasChildren = false) =>
        new(
            new MenuCategoryRepositoryStub(category, hasChildren),
            unitOfWork,
            new StubTimeProvider(UtcNow),
            new PublicMenuCacheInvalidatorStub());

    private static MenuCategory CreateCategory()
    {
        var result = MenuCategory.Create(
            MenuCategoryId.New(),
            Guid.CreateVersion7(),
            null,
            "Category",
            1,
            UtcNow.AddHours(-1));
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private sealed class MenuCategoryRepositoryStub(
        MenuCategory category,
        bool hasChildren)
        : IMenuCategoryRepository
    {
        public void Add(MenuCategory categoryToAdd) =>
            throw new NotSupportedException();

        public Task<MenuCategory?> GetByIdAsync(
            MenuCategoryId categoryId,
            CancellationToken cancellationToken) =>
            Task.FromResult<MenuCategory?>(category);

        public Task<bool> HasChildrenAsync(
            MenuCategoryId categoryId,
            CancellationToken cancellationToken) =>
            Task.FromResult(hasChildren);

        public Task<bool> HasPublishedChildrenAsync(
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

    private sealed class StubTimeProvider(DateTimeOffset utcNow)
        : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
