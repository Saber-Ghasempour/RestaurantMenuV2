using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Domain.UnitTests.Restaurants;

public sealed class RestaurantSlugTests
{
    [Fact]
    public void SlugShouldNormalizeAndAvoidNoOpChanges()
    {
        var restaurant = Create();
        Assert.True(restaurant.ChangeSlug(" Cafe-123 ").IsSuccess);
        Assert.Equal("cafe-123", restaurant.Slug);
        Assert.Equal(2, restaurant.Version);
        restaurant.ChangeSlug("CAFE-123");
        Assert.Equal(2, restaurant.Version);
        Assert.Single(restaurant.DomainEvents.OfType<RestaurantSlugChangedDomainEvent>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("-cafe")]
    [InlineData("cafe-")]
    [InlineData("cafe--bar")]
    [InlineData("cafe bar")]
    [InlineData("cafe/bar")]
    [InlineData("café")]
    public void InvalidSlugShouldNotMutate(string? slug)
    {
        var restaurant = Create();
        restaurant.ChangeSlug("original");
        Assert.True(restaurant.ChangeSlug(slug).IsFailure);
        Assert.Equal("original", restaurant.Slug);
        Assert.Equal(2, restaurant.Version);
    }

    [Fact]
    public void MaximumLengthShouldBeEnforced()
    {
        var restaurant = Create();
        Assert.True(restaurant.ChangeSlug(new string('a', 80)).IsSuccess);
        Assert.True(restaurant.ChangeSlug(new string('a', 81)).IsFailure);
    }

    private static Restaurant Create() =>
        Restaurant.Create(RestaurantId.New(), "Cafe", DateTimeOffset.UtcNow).Value;
}
