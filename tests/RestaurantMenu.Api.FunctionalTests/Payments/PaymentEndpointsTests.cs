using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RestaurantMenu.Api.FunctionalTests.Infrastructure;

namespace RestaurantMenu.Api.FunctionalTests.Payments;

public sealed class PaymentEndpointsTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task RestaurantOnboardsAndGuestPaysOnlyOwnedOrder()
    {
        await factory.MigrateDatabaseAsync();
        await factory.MigrateOrderingDatabaseAsync();
        await factory.MigratePaymentsDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync(
            $"Payments {Guid.NewGuid():N}", DateTimeOffset.UtcNow);
        var restaurantId = restaurant.Id.Value;
        var guest = await factory.SeedGuestOrderAsync(restaurantId, Guid.NewGuid());
        using var client = factory.CreateClient();

        using var onboarding = await client.PostAsJsonAsync(
            $"/api/restaurants/{restaurantId}/payments/onboarding",
            new
            {
                Country = "PT",
                Currency = "EUR",
                RefreshUrl = "https://restaurant.example/payments/refresh",
                ReturnUrl = "https://restaurant.example/payments/return"
            });
        Assert.Equal(HttpStatusCode.OK, onboarding.StatusCode);
        using var enabled = await client.PostAsync(
            $"/api/restaurants/{restaurantId}/payments/enable", null);
        Assert.Equal(HttpStatusCode.NoContent, enabled.StatusCode);

        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"/api/public/orders/{guest.Order.Id.Value}/payments")
        {
            Content = JsonContent.Create(new { TipAmountMinor = 100 })
        };
        request.Headers.Add("X-Dining-Session", guest.Token);
        using var created = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var response = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(600, response.GetProperty("grossAmountMinor").GetInt64());
        Assert.Equal(18, response.GetProperty("platformFeeAmountMinor").GetInt64());
        Assert.Equal("EUR", response.GetProperty("currency").GetString());

        using var conflict = new HttpRequestMessage(HttpMethod.Post,
            $"/api/public/orders/{Guid.NewGuid()}/payments")
        {
            Content = JsonContent.Create(new { TipAmountMinor = 0 })
        };
        conflict.Headers.Add("X-Dining-Session", guest.Token);
        conflict.Headers.Add("Cookie", "rm_dining_session=different-capability");
        using var denied = await client.SendAsync(conflict);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
    }
}
