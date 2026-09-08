using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Branches.CreateBranch;
using RestaurantMenu.Restaurants.Application.Branches.UpdateBranch;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Application.UnitTests.Branches;

public sealed class BranchCommandHandlerTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 9, 8, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateShouldPersistValidBranchForExistingRestaurant()
    {
        var restaurant = Restaurant.Create(RestaurantId.New(), "Restaurant", UtcNow).Value;
        var restaurants = new RestaurantRepositoryStub(restaurant);
        var branches = new BranchRepositoryStub();
        var unitOfWork = new UnitOfWorkSpy();
        var handler = new CreateBranchCommandHandler(
            restaurants, branches, unitOfWork, new StubTimeProvider(UtcNow));

        var result = await handler.Handle(
            new CreateBranchCommand(
                restaurant.Id, " Downtown ", "downtown", null, null, null,
                null, null, "pt", 38.72m, -9.13m, "Europe/Lisbon"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(branches.Added);
        Assert.Equal(restaurant.Id, branches.Added.RestaurantId);
        Assert.Equal("Downtown", branches.Added.Name);
        Assert.Equal(1, unitOfWork.Calls);
    }

    [Fact]
    public async Task CreateShouldNotPersistWhenRestaurantDoesNotExist()
    {
        var restaurantId = RestaurantId.New();
        var branches = new BranchRepositoryStub();
        var unitOfWork = new UnitOfWorkSpy();
        var handler = new CreateBranchCommandHandler(
            new RestaurantRepositoryStub(null), branches, unitOfWork,
            new StubTimeProvider(UtcNow));

        var result = await handler.Handle(
            new CreateBranchCommand(
                restaurantId, "Branch", null, null, null, null, null, null,
                null, null, null, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(RestaurantErrors.NotFound(restaurantId), result.Error);
        Assert.Null(branches.Added);
        Assert.Equal(0, unitOfWork.Calls);
    }

    [Fact]
    public async Task UpdateShouldRejectStaleVersionWithoutSaving()
    {
        var branch = Branch.Create(
            BranchId.New(), RestaurantId.New(), "Branch", null, null, null,
            null, null, null, null, null, null, null, UtcNow).Value;
        var branches = new BranchRepositoryStub(branch);
        var unitOfWork = new UnitOfWorkSpy();
        var handler = new UpdateBranchCommandHandler(branches, unitOfWork);

        var result = await handler.Handle(
            new UpdateBranchCommand(
                branch.RestaurantId, branch.Id, "Changed", null, null, null,
                null, null, null, null, null, null, null, 42),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(BranchErrors.VersionConflict(branch.Id), result.Error);
        Assert.Equal("Branch", branch.Name);
        Assert.Equal(0, unitOfWork.Calls);
    }

    private sealed class RestaurantRepositoryStub(Restaurant? restaurant)
        : IRestaurantRepository
    {
        public void Add(Restaurant value) => throw new NotSupportedException();
        public Task<Restaurant?> GetByIdAsync(
            RestaurantId restaurantId,
            CancellationToken cancellationToken) => Task.FromResult(restaurant);
    }

    private sealed class BranchRepositoryStub(Branch? branch = null)
        : IBranchRepository
    {
        public Branch? Added { get; private set; }
        public void Add(Branch value) => Added = value;
        public Task<Branch?> GetByIdAsync(
            RestaurantId restaurantId,
            BranchId branchId,
            CancellationToken cancellationToken) => Task.FromResult(branch);
    }

    private sealed class UnitOfWorkSpy : IUnitOfWork
    {
        public int Calls { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(1);
        }
    }

    private sealed class StubTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
