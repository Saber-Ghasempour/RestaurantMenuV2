using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Restaurants.CreateRestaurant;
using RestaurantMenu.Restaurants.Domain.Memberships;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Application.UnitTests.Restaurants.CreateRestaurant;

public sealed class CreateRestaurantCommandHandlerTests
{
    private const string OwnerSubject = "keycloak-owner-123";

    private static readonly DateTimeOffset UtcNow =
        new(2026, 9, 6, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleShouldCreateRestaurantAndPersistChanges()
    {
        var repository = new RestaurantRepositorySpy();
        var membershipRepository = new RestaurantMembershipRepositorySpy();
        var unitOfWork = new UnitOfWorkSpy();
        var timeProvider = new StubTimeProvider(UtcNow);

        var handler = new CreateRestaurantCommandHandler(
            repository,
            membershipRepository,
            unitOfWork,
            timeProvider);

        var command = new CreateRestaurantCommand(
            " Coffee Menu ",
            OwnerSubject);

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(default, result.Value);

        Assert.NotNull(repository.AddedRestaurant);
        Assert.Equal(result.Value, repository.AddedRestaurant.Id);
        Assert.Equal("Coffee Menu", repository.AddedRestaurant.Name);
        Assert.Equal(UtcNow, repository.AddedRestaurant.CreatedAtUtc);

        Assert.Equal(1, repository.AddCallCount);
        Assert.NotNull(membershipRepository.AddedMembership);
        Assert.Equal(
            result.Value,
            membershipRepository.AddedMembership.RestaurantId);
        Assert.Equal(
            OwnerSubject,
            membershipRepository.AddedMembership.Subject);
        Assert.Equal(
            RestaurantMembershipRole.Owner,
            membershipRepository.AddedMembership.Role);
        Assert.Equal(1, membershipRepository.AddCallCount);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleShouldNotPersistWhenNameIsInvalid(
    string? name)
    {
        var repository = new RestaurantRepositorySpy();
        var membershipRepository = new RestaurantMembershipRepositorySpy();
        var unitOfWork = new UnitOfWorkSpy();
        var timeProvider = new StubTimeProvider(UtcNow);

        var handler = new CreateRestaurantCommandHandler(
            repository,
            membershipRepository,
            unitOfWork,
            timeProvider);

        var command = new CreateRestaurantCommand(
            name,
            OwnerSubject);

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(RestaurantErrors.NameRequired, result.Error);

        Assert.Null(repository.AddedRestaurant);
        Assert.Null(membershipRepository.AddedMembership);
        Assert.Equal(0, repository.AddCallCount);
        Assert.Equal(0, membershipRepository.AddCallCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private sealed class RestaurantRepositorySpy :
        IRestaurantRepository
    {
        public Restaurant? AddedRestaurant { get; private set; }

        public int AddCallCount { get; private set; }

        public void Add(Restaurant restaurant)
        {
            AddedRestaurant = restaurant;
            AddCallCount++;
        }

        public Task<Restaurant?> GetByIdAsync(
            RestaurantId restaurantId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class RestaurantMembershipRepositorySpy :
        IRestaurantMembershipRepository
    {
        public RestaurantMembership? AddedMembership { get; private set; }

        public int AddCallCount { get; private set; }

        public void Add(RestaurantMembership membership)
        {
            AddedMembership = membership;
            AddCallCount++;
        }
    }

    private sealed class UnitOfWorkSpy : IUnitOfWork
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
        DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() =>
            utcNow;
    }
}
