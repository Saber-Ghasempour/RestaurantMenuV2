using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Abstractions.Branches;
using RestaurantMenu.Catalog.Application.Abstractions.Caching;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Publications;
using RestaurantMenu.Catalog.Application.Publications.SetBranchCategoryPublications;
using RestaurantMenu.Catalog.Application.PublicMenus.GetPublicBranchMenu;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Publications;

namespace RestaurantMenu.Catalog.Application.UnitTests.Publications;

public sealed class SetBranchCategoryPublicationsCommandHandlerTests
{
    [Fact]
    public async Task HandleShouldReplaceCompleteSetAndInvalidateAfterCommit()
    {
        var restaurantId = Guid.CreateVersion7();
        var branchId = Guid.CreateVersion7();
        var parent = MenuCategoryId.New();
        var child = MenuCategoryId.New();
        var calls = new List<string>();
        var repository = new RepositoryStub(
            [new(parent, null), new(child, parent)],
            []);
        var handler = new SetBranchCategoryPublicationsCommandHandler(
            new BranchCheckerStub(true),
            repository,
            new UnitOfWorkSpy(calls),
            new CacheSpy(calls),
            TimeProvider.System);

        var result = await handler.Handle(
            new SetBranchCategoryPublicationsCommand(
                restaurantId,
                branchId,
                [new(parent.Value, true, 2), new(child.Value, true, 1)]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        Assert.Equal(2, repository.Added.Count);
        Assert.Equal(["commit", "invalidate"], calls);
    }

    [Fact]
    public async Task HandleShouldRejectIncompleteOrCrossTenantCategorySet()
    {
        var category = MenuCategoryId.New();
        var repository = new RepositoryStub([new(category, null)], []);
        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new SetBranchCategoryPublicationsCommand(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                [new(Guid.CreateVersion7(), true, null)]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            global::RestaurantMenu.Catalog.Application.Publications.BranchCategoryPublicationErrors.CompleteSetRequired,
            result.Error);
    }

    [Fact]
    public async Task HandleShouldRequirePublishedParentForPublishedChild()
    {
        var parent = MenuCategoryId.New();
        var child = MenuCategoryId.New();
        var repository = new RepositoryStub(
            [new(parent, null), new(child, parent)],
            []);
        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new SetBranchCategoryPublicationsCommand(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                [new(parent.Value, false, null), new(child.Value, true, null)]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "Catalog.PublishedParentRequired",
            result.Error.Code);
    }

    [Fact]
    public async Task HandleShouldNotCommitOrInvalidateEquivalentSet()
    {
        var restaurantId = Guid.CreateVersion7();
        var branchId = Guid.CreateVersion7();
        var categoryId = MenuCategoryId.New();
        var publication = BranchCategoryPublication.Create(
            restaurantId,
            branchId,
            categoryId,
            true,
            4,
            DateTimeOffset.UtcNow).Value;
        var calls = new List<string>();
        var handler = new SetBranchCategoryPublicationsCommandHandler(
            new BranchCheckerStub(true),
            new RepositoryStub([new(categoryId, null)], [publication]),
            new UnitOfWorkSpy(calls),
            new CacheSpy(calls),
            TimeProvider.System);

        var result = await handler.Handle(
            new SetBranchCategoryPublicationsCommand(
                restaurantId,
                branchId,
                [new(categoryId.Value, true, 4)]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
        Assert.Empty(calls);
        Assert.Equal(1, publication.Version);
    }

    private static SetBranchCategoryPublicationsCommandHandler CreateHandler(
        RepositoryStub repository) =>
        new(
            new BranchCheckerStub(true),
            repository,
            new UnitOfWorkSpy([]),
            new CacheSpy([]),
            TimeProvider.System);

    private sealed class BranchCheckerStub(bool exists)
        : IBranchExistenceChecker
    {
        public Task<bool> ExistsAsync(Guid restaurantId, Guid branchId,
            CancellationToken cancellationToken) => Task.FromResult(exists);
    }

    private sealed class RepositoryStub(
        IReadOnlyList<PublicationCategory> categories,
        IReadOnlyList<BranchCategoryPublication> publications)
        : IBranchCategoryPublicationRepository
    {
        public List<BranchCategoryPublication> Added { get; } = [];

        public Task<IReadOnlyList<PublicationCategory>> GetCategoriesAsync(
            Guid restaurantId, CancellationToken cancellationToken) =>
            Task.FromResult(categories);

        public Task<IReadOnlyList<BranchCategoryPublication>> GetByBranchAsync(
            Guid restaurantId, Guid branchId,
            CancellationToken cancellationToken) => Task.FromResult(publications);

        public void Add(BranchCategoryPublication publication) => Added.Add(publication);
    }

    private sealed class UnitOfWorkSpy(List<string> calls) : ICatalogUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            calls.Add("commit");
            return Task.FromResult(1);
        }
    }

    private sealed class CacheSpy(List<string> calls) : IPublicBranchMenuCache
    {
        public Task<PublicBranchMenuResponse?> GetAsync(Guid restaurantId,
            Guid branchId, CancellationToken cancellationToken) =>
            Task.FromResult<PublicBranchMenuResponse?>(null);

        public Task SetAsync(PublicBranchMenuResponse menu,
            CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RemoveAsync(Guid restaurantId, Guid branchId,
            CancellationToken cancellationToken)
        {
            calls.Add("invalidate");
            return Task.CompletedTask;
        }
    }
}
