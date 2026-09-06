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
}