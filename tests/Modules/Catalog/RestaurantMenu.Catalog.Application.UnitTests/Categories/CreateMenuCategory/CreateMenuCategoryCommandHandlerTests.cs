using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Abstractions.Restaurants;
using RestaurantMenu.Catalog.Application.Categories;
using RestaurantMenu.Catalog.Application.Categories.CreateMenuCategory;
using RestaurantMenu.Catalog.Domain.Categories;

namespace RestaurantMenu.Catalog.Application.UnitTests.Categories.CreateMenuCategory;

public sealed class CreateMenuCategoryCommandHandlerTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 9, 7, 11, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleShouldCreateAndPersistCategory()
    {
        var restaurantId = Guid.CreateVersion7();
        var repository = new MenuCategoryRepositoryStub();
        var unitOfWork = new UnitOfWorkSpy();
        var handler = CreateHandler(
            repository,
            unitOfWork,
            restaurantExists: true);

        var result = await handler.Handle(
            new CreateMenuCategoryCommand(
                restaurantId,
                null,
                " Hot Drinks ",
                10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedCategory);
        Assert.Equal(result.Value, repository.AddedCategory.Id);
        Assert.Equal(restaurantId, repository.AddedCategory.RestaurantId);
        Assert.Equal("Hot Drinks", repository.AddedCategory.Name);
        Assert.Equal(UtcNow, repository.AddedCategory.CreatedAtUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnNotFoundWhenRestaurantDoesNotExist()
    {
        var restaurantId = Guid.CreateVersion7();
        var repository = new MenuCategoryRepositoryStub();
        var unitOfWork = new UnitOfWorkSpy();
        var handler = CreateHandler(
            repository,
            unitOfWork,
            restaurantExists: false);

        var result = await handler.Handle(
            new CreateMenuCategoryCommand(
                restaurantId,
                null,
                "Drinks",
                0),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            MenuCategoryApplicationErrors.RestaurantNotFound(
                restaurantId),
            result.Error);
        Assert.Null(repository.AddedCategory);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldRejectParentFromAnotherRestaurant()
    {
        var restaurantId = Guid.CreateVersion7();
        var parent = CreateCategory(Guid.CreateVersion7());
        var repository = new MenuCategoryRepositoryStub(parent);
        var unitOfWork = new UnitOfWorkSpy();
        var handler = CreateHandler(
            repository,
            unitOfWork,
            restaurantExists: true);

        var result = await handler.Handle(
            new CreateMenuCategoryCommand(
                restaurantId,
                parent.Id.Value,
                "Coffee",
                0),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            MenuCategoryApplicationErrors.ParentCategoryNotFound(
                parent.Id.Value),
            result.Error);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldNotPersistInvalidCategory()
    {
        var repository = new MenuCategoryRepositoryStub();
        var unitOfWork = new UnitOfWorkSpy();
        var handler = CreateHandler(
            repository,
            unitOfWork,
            restaurantExists: true);

        var result = await handler.Handle(
            new CreateMenuCategoryCommand(
                Guid.CreateVersion7(),
                null,
                " ",
                0),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(MenuCategoryErrors.NameRequired, result.Error);
        Assert.Null(repository.AddedCategory);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private static CreateMenuCategoryCommandHandler CreateHandler(
        MenuCategoryRepositoryStub repository,
        UnitOfWorkSpy unitOfWork,
        bool restaurantExists)
    {
        return new CreateMenuCategoryCommandHandler(
            repository,
            new RestaurantExistenceCheckerStub(
                restaurantExists),
            unitOfWork,
            new StubTimeProvider(UtcNow));
    }

    private static MenuCategory CreateCategory(Guid restaurantId)
    {
        var result = MenuCategory.Create(
            MenuCategoryId.New(),
            restaurantId,
            null,
            "Parent",
            0,
            UtcNow);
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private sealed class MenuCategoryRepositoryStub(
        MenuCategory? category = null)
        : IMenuCategoryRepository
    {
        public MenuCategory? AddedCategory { get; private set; }

        public void Add(MenuCategory categoryToAdd) =>
            AddedCategory = categoryToAdd;

        public Task<MenuCategory?> GetByIdAsync(
            MenuCategoryId categoryId,
            CancellationToken cancellationToken) =>
            Task.FromResult(category);

        public Task<bool> HasChildrenAsync(
            MenuCategoryId categoryId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }

    private sealed class RestaurantExistenceCheckerStub(
        bool exists)
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

    private sealed class StubTimeProvider(
        DateTimeOffset utcNow)
        : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
