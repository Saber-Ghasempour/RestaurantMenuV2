using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Domain.UnitTests.Restaurants;

public sealed class RestaurantBrandingTests
{
    [Fact]
    public void BrandingShouldValidateReferencesAndVersionChanges()
    {
        var restaurant = Restaurant.Create(RestaurantId.New(), "Cafe", DateTimeOffset.UtcNow).Value;
        Assert.Equal(RestaurantErrors.InvalidMediaReference, restaurant.SetBranding(Guid.Empty, null).Error);
        var logo = Guid.CreateVersion7();
        Assert.True(restaurant.SetBranding(logo, null).IsSuccess);
        Assert.Equal(logo, restaurant.LogoMediaId);
        Assert.Equal(2, restaurant.Version);
        restaurant.SetBranding(logo, null);
        Assert.Equal(2, restaurant.Version);
    }
}
