using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Domain.UnitTests.Restaurants;

public sealed class RestaurantTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 9, 6, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void CreateShouldReturnRestaurantAndRaiseDomainEvent()
    {
        var restaurantId = RestaurantId.New();

        var result = Restaurant.Create(
            restaurantId,
            " Coffee Menu ",
            CreatedAtUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(restaurantId, result.Value.Id);
        Assert.Equal("Coffee Menu", result.Value.Name);
        Assert.Equal(CreatedAtUtc, result.Value.CreatedAtUtc);
        Assert.Equal(1, result.Value.Version);

        var domainEvent = Assert.Single(result.Value.DomainEvents);
        var restaurantCreated =
            Assert.IsType<RestaurantCreatedDomainEvent>(domainEvent);

        Assert.Equal(restaurantId, restaurantCreated.RestaurantId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateShouldFailWhenNameIsMissing(string? name)
    {
        var result = Restaurant.Create(
            RestaurantId.New(),
            name,
            CreatedAtUtc);

        Assert.True(result.IsFailure);
        Assert.Equal(RestaurantErrors.NameRequired, result.Error);
    }

    [Fact]
    public void CreateShouldFailWhenNameExceedsMaximumLength()
    {
        var name = new string(
            'a',
            Restaurant.MaxNameLength + 1);

        var result = Restaurant.Create(
            RestaurantId.New(),
            name,
            CreatedAtUtc);

        Assert.True(result.IsFailure);
        Assert.Equal(RestaurantErrors.NameTooLong, result.Error);
    }

    [Fact]
    public void CreateShouldSucceedWhenNameHasMaximumLength()
    {
        var name = new string(
            'a',
            Restaurant.MaxNameLength);

        var result = Restaurant.Create(
            RestaurantId.New(),
            name,
            CreatedAtUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(Restaurant.MaxNameLength, result.Value.Name.Length);
    }

    [Fact]
    public void RenameShouldNormalizeNameIncrementVersionAndRaiseDomainEvent()
    {
        var restaurant = CreateRestaurant();
        restaurant.ClearDomainEvents();

        var result = restaurant.Rename(" Updated Menu ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Menu", restaurant.Name);
        Assert.Equal(2, restaurant.Version);

        var domainEvent = Assert.Single(restaurant.DomainEvents);
        var renamed =
            Assert.IsType<RestaurantRenamedDomainEvent>(
                domainEvent);

        Assert.Equal(restaurant.Id, renamed.RestaurantId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RenameShouldFailWithoutChangingRestaurantWhenNameIsMissing(
        string? name)
    {
        var restaurant = CreateRestaurant();
        restaurant.ClearDomainEvents();

        var result = restaurant.Rename(name);

        Assert.True(result.IsFailure);
        Assert.Equal(RestaurantErrors.NameRequired, result.Error);
        Assert.Equal("Original Menu", restaurant.Name);
        Assert.Equal(1, restaurant.Version);
        Assert.Empty(restaurant.DomainEvents);
    }

    [Fact]
    public void RenameShouldFailWithoutChangingRestaurantWhenNameIsTooLong()
    {
        var restaurant = CreateRestaurant();
        restaurant.ClearDomainEvents();

        var result = restaurant.Rename(
            new string(
                'a',
                Restaurant.MaxNameLength + 1));

        Assert.True(result.IsFailure);
        Assert.Equal(RestaurantErrors.NameTooLong, result.Error);
        Assert.Equal("Original Menu", restaurant.Name);
        Assert.Equal(1, restaurant.Version);
        Assert.Empty(restaurant.DomainEvents);
    }

    [Fact]
    public void RenameShouldNotChangeVersionWhenNormalizedNameIsUnchanged()
    {
        var restaurant = CreateRestaurant();
        restaurant.ClearDomainEvents();

        var result = restaurant.Rename(" Original Menu ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Original Menu", restaurant.Name);
        Assert.Equal(1, restaurant.Version);
        Assert.Empty(restaurant.DomainEvents);
    }

    private static Restaurant CreateRestaurant()
    {
        var result = Restaurant.Create(
            RestaurantId.New(),
            "Original Menu",
            CreatedAtUtc);

        Assert.True(result.IsSuccess);

        return result.Value;
    }
}
