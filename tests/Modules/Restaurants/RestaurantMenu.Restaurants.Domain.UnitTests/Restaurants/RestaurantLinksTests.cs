using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Domain.UnitTests.Restaurants;

public sealed class RestaurantLinksTests
{
    [Fact]
    public void LinksShouldNormalizeClearAndAvoidNoOpVersionChanges()
    {
        var restaurant = Create();
        Assert.True(restaurant.UpdateLinks(" https://example.com ", "https://instagram.com/cafe",
            "https://facebook.com/cafe", "https://wa.me/12345", "https://t.me/cafe", "https://x.com/cafe").IsSuccess);
        Assert.Equal("https://example.com", restaurant.WebsiteUrl);
        Assert.Equal(2, restaurant.Version);
        restaurant.UpdateLinks("https://example.com", restaurant.InstagramUrl, restaurant.FacebookUrl,
            restaurant.WhatsAppUrl, restaurant.TelegramUrl, restaurant.TwitterUrl);
        Assert.Equal(2, restaurant.Version);
        Assert.Single(restaurant.DomainEvents.OfType<RestaurantLinksUpdatedDomainEvent>());
        restaurant.UpdateLinks(" ", null, "", null, null, null);
        Assert.Null(restaurant.WebsiteUrl);
        Assert.Null(restaurant.InstagramUrl);
        Assert.Null(restaurant.FacebookUrl);
        Assert.Null(restaurant.WhatsAppUrl);
        Assert.Null(restaurant.TelegramUrl);
        Assert.Null(restaurant.TwitterUrl);
        Assert.Equal(3, restaurant.Version);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,test")]
    [InlineData("http://example.com")]
    [InlineData("//example.com")]
    [InlineData("/relative")]
    [InlineData("https://user:password@example.com")]
    [InlineData("https://")]
    [InlineData("https://example.com/a b")]
    [InlineData("https://example.com/a\nb")]
    public void InvalidLinkShouldLeaveAllFieldsUnchanged(string invalidLink)
    {
        var restaurant = Create();
        restaurant.UpdateLinks("https://original.example", null, null, null, null, null);
        var result = restaurant.UpdateLinks("https://changed.example", invalidLink, null, null, null, null);
        Assert.True(result.IsFailure);
        Assert.Equal("Restaurants.InvalidLink", result.Error.Code);
        Assert.Equal("https://original.example", restaurant.WebsiteUrl);
        Assert.Null(restaurant.InstagramUrl);
        Assert.Equal(2, restaurant.Version);
    }

    [Fact]
    public void LengthBoundaryShouldBeEnforced()
    {
        var restaurant = Create();
        const string prefix = "https://example.com/";
        var maximum = prefix + new string('a', 2048 - prefix.Length);
        Assert.True(restaurant.UpdateLinks(maximum, null, null, null, null, null).IsSuccess);
        Assert.True(restaurant.UpdateLinks(maximum + "a", null, null, null, null, null).IsFailure);
        Assert.Equal(maximum, restaurant.WebsiteUrl);
    }

    private static Restaurant Create() =>
        Restaurant.Create(RestaurantId.New(), "Cafe", DateTimeOffset.UtcNow).Value;
}
