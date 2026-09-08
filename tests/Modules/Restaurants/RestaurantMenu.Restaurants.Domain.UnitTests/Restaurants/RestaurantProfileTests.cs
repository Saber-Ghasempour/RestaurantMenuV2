using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Domain.UnitTests.Restaurants;

public sealed class RestaurantProfileTests
{
    [Fact]
    public void UpdateShouldNormalizeAndBeIdempotent()
    {
        var restaurant = Restaurant.Create(RestaurantId.New(), "Cafe", DateTimeOffset.UtcNow).Value;
        Assert.True(restaurant.UpdateProfile(" Cafe description ", " About us ", " Lisbon ").IsSuccess);
        Assert.Equal("Cafe description", restaurant.Description);
        Assert.Equal("About us", restaurant.About);
        Assert.Equal("Lisbon", restaurant.Address);
        Assert.Equal(2, restaurant.Version);
        Assert.Single(restaurant.DomainEvents.OfType<RestaurantProfileUpdatedDomainEvent>());
        restaurant.UpdateProfile("Cafe description", "About us", "Lisbon");
        Assert.Equal(2, restaurant.Version);
        Assert.Single(restaurant.DomainEvents.OfType<RestaurantProfileUpdatedDomainEvent>());
        restaurant.UpdateProfile(" ", null, "");
        Assert.Null(restaurant.Description);
        Assert.Null(restaurant.About);
        Assert.Null(restaurant.Address);
        Assert.Equal(3, restaurant.Version);
    }

    [Theory]
    [InlineData(501, 10, 10)]
    [InlineData(10, 4001, 10)]
    [InlineData(10, 10, 501)]
    public void InvalidProfileShouldNotPartiallyMutate(int descriptionLength, int aboutLength, int addressLength)
    {
        var restaurant = Restaurant.Create(RestaurantId.New(), "Cafe", DateTimeOffset.UtcNow).Value;
        restaurant.UpdateProfile("original", "original", "original");
        var result = restaurant.UpdateProfile(
            new string('a', descriptionLength), new string('b', aboutLength), new string('c', addressLength));
        Assert.True(result.IsFailure);
        Assert.Equal("original", restaurant.Description);
        Assert.Equal("original", restaurant.About);
        Assert.Equal("original", restaurant.Address);
        Assert.Equal(2, restaurant.Version);
    }

    [Fact]
    public void MaximumLengthsShouldBeAccepted()
    {
        var restaurant = Restaurant.Create(RestaurantId.New(), "Cafe", DateTimeOffset.UtcNow).Value;
        Assert.True(restaurant.UpdateProfile(
            new string('a', 500), new string('b', 4000), new string('c', 500)).IsSuccess);
    }
}
