using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Restaurants.DeleteRestaurant;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Application.UnitTests.Restaurants.DeleteRestaurant;

public sealed class DeleteRestaurantCommandHandlerTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 9, 6, 22, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleShouldSoftDeleteRestaurant()
    {
        var restaurant = CreateRestaurant();
        var unitOfWork = new UnitOfWorkStub();
        var handler = CreateHandler(restaurant, unitOfWork);

        var result = await handler.Handle(
            new DeleteRestaurantCommand(restaurant.Id, 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(restaurant.Id, result.Value);
        Assert.True(restaurant.IsDeleted);
        Assert.Equal(UtcNow, restaurant.DeletedAtUtc);
        Assert.Equal(2, restaurant.Version);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnNotFoundWhenRestaurantDoesNotExist()
    {
        var restaurantId = RestaurantId.New();
        var unitOfWork = new UnitOfWorkStub();
        var handler = CreateHandler(null, unitOfWork);

        var result = await handler.Handle(
            new DeleteRestaurantCommand(restaurantId, 1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            RestaurantErrors.NotFound(restaurantId),
            result.Error);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnConflictForStaleVersion()
    {
        var restaurant = CreateRestaurant();
        var unitOfWork = new UnitOfWorkStub();
        var handler = CreateHandler(restaurant, unitOfWork);

        var result = await handler.Handle(
            new DeleteRestaurantCommand(restaurant.Id, 42),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            RestaurantErrors.VersionConflict(restaurant.Id),
            result.Error);
        Assert.False(restaurant.IsDeleted);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnConflictForConcurrentDatabaseUpdate()
    {
        var restaurant = CreateRestaurant();
        var unitOfWork = new UnitOfWorkStub(
            new ConcurrencyException("Concurrent update."));
        var handler = CreateHandler(restaurant, unitOfWork);

        var result = await handler.Handle(
            new DeleteRestaurantCommand(restaurant.Id, 1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            RestaurantErrors.VersionConflict(restaurant.Id),
            result.Error);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    private static DeleteRestaurantCommandHandler CreateHandler(
        Restaurant? restaurant,
        UnitOfWorkStub unitOfWork)
    {
        return new DeleteRestaurantCommandHandler(
            new RestaurantRepositoryStub(restaurant),
            unitOfWork,
            new StubTimeProvider(UtcNow));
    }

    private static Restaurant CreateRestaurant()
    {
        var result = Restaurant.Create(
            RestaurantId.New(),
            "Restaurant",
            UtcNow.AddHours(-1));
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private sealed class RestaurantRepositoryStub(
        Restaurant? restaurant)
        : IRestaurantRepository
    {
        public void Add(Restaurant restaurantToAdd) =>
            throw new NotSupportedException();

        public Task<Restaurant?> GetByIdAsync(
            RestaurantId restaurantId,
            CancellationToken cancellationToken) =>
            Task.FromResult(restaurant);
    }

    private sealed class UnitOfWorkStub(
        Exception? exception = null)
        : IUnitOfWork
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

    private sealed class StubTimeProvider(
        DateTimeOffset utcNow)
        : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
