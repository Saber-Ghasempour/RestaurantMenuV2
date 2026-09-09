using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Domain.UnitTests.Restaurants;

public sealed class RestaurantDefaultsTests
{
    [Fact]
    public void CreateShouldUseStableDefaults()
    {
        var restaurant = Restaurant.Create(
            RestaurantId.New(), "Cafe", DateTimeOffset.UtcNow).Value;

        Assert.Equal("USD", restaurant.DefaultCurrency);
        Assert.Equal("en-US", restaurant.DefaultLocale);
        Assert.Equal("Etc/UTC", restaurant.TimeZoneId);
    }

    [Fact]
    public void UpdateDefaultsShouldNormalizeValuesAndBeIdempotent()
    {
        var restaurant = CreateRestaurant();
        restaurant.ClearDomainEvents();

        var result = restaurant.UpdateDefaults(" eur ", " pt-PT ", " Europe/Lisbon ");

        Assert.True(result.IsSuccess);
        Assert.Equal("EUR", restaurant.DefaultCurrency);
        Assert.Equal("pt-PT", restaurant.DefaultLocale);
        Assert.Equal("Europe/Lisbon", restaurant.TimeZoneId);
        Assert.Equal(2, restaurant.Version);
        Assert.IsType<RestaurantDefaultsUpdatedDomainEvent>(Assert.Single(restaurant.DomainEvents));

        restaurant.ClearDomainEvents();
        result = restaurant.UpdateDefaults("EUR", "pt-PT", "Europe/Lisbon");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, restaurant.Version);
        Assert.Empty(restaurant.DomainEvents);
    }

    [Theory]
    [InlineData("ZZZ", "en-US", "Etc/UTC", "Restaurants.InvalidDefaultCurrency")]
    [InlineData("USD", "not_a_locale", "Etc/UTC", "Restaurants.InvalidDefaultLocale")]
    [InlineData("USD", "en-US", "Mars/Olympus", "Restaurants.InvalidTimeZone")]
    public void UpdateDefaultsShouldRejectInvalidValuesAtomically(
        string currency,
        string locale,
        string timeZoneId,
        string expectedCode)
    {
        var restaurant = CreateRestaurant();

        var result = restaurant.UpdateDefaults(currency, locale, timeZoneId);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
        Assert.Equal("USD", restaurant.DefaultCurrency);
        Assert.Equal("en-US", restaurant.DefaultLocale);
        Assert.Equal("Etc/UTC", restaurant.TimeZoneId);
        Assert.Equal(1, restaurant.Version);
    }

    private static Restaurant CreateRestaurant() =>
        Restaurant.Create(RestaurantId.New(), "Cafe", DateTimeOffset.UtcNow).Value;
}
