using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Restaurants.UpdateRestaurantDefaults;
using RestaurantMenu.Restaurants.Application.UnitTests.TestDoubles;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Application.UnitTests.Restaurants.UpdateRestaurantDefaults;

public sealed class UpdateRestaurantDefaultsCommandHandlerTests
{
    [Fact]
    public async Task HandleShouldCommitThenInvalidateRestaurantCache()
    {
        var restaurant = Restaurant.Create(
            RestaurantId.New(), "Cafe", DateTimeOffset.UtcNow).Value;
        var unitOfWork = new UnitOfWorkStub();
        var cache = new RestaurantCacheStub();
        var handler = new UpdateRestaurantDefaultsCommandHandler(
            new RepositoryStub(restaurant), unitOfWork, cache,
            new PublicMenuInvalidatorStub());

        var result = await handler.Handle(new UpdateRestaurantDefaultsCommand(
            restaurant.Id, "EUR", "pt-PT", "Europe/Lisbon", 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
        Assert.Equal(1, unitOfWork.CallCount);
        Assert.Equal(1, cache.RemoveCallCount);
    }

    [Fact]
    public async Task HandleShouldNotSaveOrInvalidateInvalidDefaults()
    {
        var restaurant = Restaurant.Create(
            RestaurantId.New(), "Cafe", DateTimeOffset.UtcNow).Value;
        var unitOfWork = new UnitOfWorkStub();
        var cache = new RestaurantCacheStub();
        var handler = new UpdateRestaurantDefaultsCommandHandler(
            new RepositoryStub(restaurant), unitOfWork, cache,
            new PublicMenuInvalidatorStub());

        var result = await handler.Handle(new UpdateRestaurantDefaultsCommand(
            restaurant.Id, "ZZZ", "pt-PT", "Europe/Lisbon", 1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.CallCount);
        Assert.Equal(0, cache.RemoveCallCount);
    }

    private sealed class RepositoryStub(Restaurant restaurant) : IRestaurantRepository
    {
        public void Add(Restaurant restaurantToAdd) => throw new NotSupportedException();

        public Task<Restaurant?> GetByIdAsync(
            RestaurantId restaurantId,
            CancellationToken cancellationToken) => Task.FromResult<Restaurant?>(restaurant);
    }

    private sealed class UnitOfWorkStub : IUnitOfWork
    {
        public int CallCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class PublicMenuInvalidatorStub :
        RestaurantMenu.Restaurants.Application.Abstractions.Caching.IRestaurantPublicMenuInvalidator
    {
        public Task InvalidateAsync(
            RestaurantId restaurantId,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
