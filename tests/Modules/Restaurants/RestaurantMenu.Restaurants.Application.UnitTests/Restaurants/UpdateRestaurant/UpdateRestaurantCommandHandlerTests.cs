using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Restaurants.UpdateRestaurant;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Application.UnitTests.Restaurants.UpdateRestaurant;

public sealed class UpdateRestaurantCommandHandlerTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 9, 6, 21, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleShouldRenameRestaurantAndReturnNewVersion()
    {
        var restaurant = CreateRestaurant();
        var repository = new RestaurantRepositoryStub(restaurant);
        var unitOfWork = new UnitOfWorkStub();
        var handler = new UpdateRestaurantCommandHandler(
            repository,
            unitOfWork);

        var result = await handler.Handle(
            new UpdateRestaurantCommand(
                restaurant.Id,
                " Updated Restaurant ",
                1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
        Assert.Equal("Updated Restaurant", restaurant.Name);
        Assert.Equal(2, restaurant.Version);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnNotFoundWhenRestaurantDoesNotExist()
    {
        var restaurantId = RestaurantId.New();
        var repository = new RestaurantRepositoryStub(null);
        var unitOfWork = new UnitOfWorkStub();
        var handler = new UpdateRestaurantCommandHandler(
            repository,
            unitOfWork);

        var result = await handler.Handle(
            new UpdateRestaurantCommand(
                restaurantId,
                "Updated Restaurant",
                1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            RestaurantErrors.NotFound(restaurantId),
            result.Error);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnConflictWhenExpectedVersionIsStale()
    {
        var restaurant = CreateRestaurant();
        var repository = new RestaurantRepositoryStub(restaurant);
        var unitOfWork = new UnitOfWorkStub();
        var handler = new UpdateRestaurantCommandHandler(
            repository,
            unitOfWork);

        var result = await handler.Handle(
            new UpdateRestaurantCommand(
                restaurant.Id,
                "Updated Restaurant",
                42),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            RestaurantErrors.VersionConflict(restaurant.Id),
            result.Error);
        Assert.Equal("Original Restaurant", restaurant.Name);
        Assert.Equal(1, restaurant.Version);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnValidationErrorWhenNameIsInvalid()
    {
        var restaurant = CreateRestaurant();
        var repository = new RestaurantRepositoryStub(restaurant);
        var unitOfWork = new UnitOfWorkStub();
        var handler = new UpdateRestaurantCommandHandler(
            repository,
            unitOfWork);

        var result = await handler.Handle(
            new UpdateRestaurantCommand(
                restaurant.Id,
                " ",
                1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(RestaurantErrors.NameRequired, result.Error);
        Assert.Equal(1, restaurant.Version);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleShouldReturnConflictWhenDatabaseDetectsConcurrentUpdate()
    {
        var restaurant = CreateRestaurant();
        var repository = new RestaurantRepositoryStub(restaurant);
        var unitOfWork = new UnitOfWorkStub(
            new ConcurrencyException(
                "The restaurant was concurrently updated."));
        var handler = new UpdateRestaurantCommandHandler(
            repository,
            unitOfWork);

        var result = await handler.Handle(
            new UpdateRestaurantCommand(
                restaurant.Id,
                "Updated Restaurant",
                1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            RestaurantErrors.VersionConflict(restaurant.Id),
            result.Error);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    private static Restaurant CreateRestaurant()
    {
        var result = Restaurant.Create(
            RestaurantId.New(),
            "Original Restaurant",
            CreatedAtUtc);

        Assert.True(result.IsSuccess);

        return result.Value;
    }

    private sealed class RestaurantRepositoryStub(
        Restaurant? restaurant)
        : IRestaurantRepository
    {
        public void Add(Restaurant restaurantToAdd)
        {
            throw new NotSupportedException();
        }

        public Task<Restaurant?> GetByIdAsync(
            RestaurantId restaurantId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(restaurant);
        }
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
}
