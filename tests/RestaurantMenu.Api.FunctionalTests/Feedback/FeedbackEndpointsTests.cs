using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RestaurantMenu.Api.FunctionalTests.Infrastructure;
using RestaurantMenu.Ordering.Domain.Orders;
namespace RestaurantMenu.Api.FunctionalTests.Feedback;
public sealed class FeedbackEndpointsTests(TestWebApplicationFactory factory):IClassFixture<TestWebApplicationFactory>
{
    [Fact] public async Task AnonymousCapabilityCanSubmitOnlyOwnedCompletedOrderAndDuplicateIsRejected()
    {
        await factory.MigrateOrderingDatabaseAsync(); await factory.MigrateFeedbackDatabaseAsync();
        var rid=Guid.NewGuid();var bid=Guid.NewGuid();var owned=await factory.SeedGuestOrderAsync(rid,bid,OrderStatus.Completed);
        var other=await factory.SeedGuestOrderAsync(rid,bid,OrderStatus.Completed); var line=owned.Order.Lines.Single().Id.Value;
        using var client=factory.CreateClient();
        using var wrong=new HttpRequestMessage(HttpMethod.Post,$"/api/public/orders/{other.Order.Id.Value}/feedback"){Content=JsonContent.Create(new{OrderLineId=line,Rating=5,Comment="great"})};wrong.Headers.Add("X-Dining-Session",owned.Token);
        Assert.Equal(HttpStatusCode.NotFound,(await client.SendAsync(wrong)).StatusCode);
        using var request=new HttpRequestMessage(HttpMethod.Post,$"/api/public/orders/{owned.Order.Id.Value}/feedback"){Content=JsonContent.Create(new{OrderLineId=line,Rating=5,Comment="great"})};request.Headers.Add("X-Dining-Session",owned.Token);
        using var created=await client.SendAsync(request);Assert.Equal(HttpStatusCode.Created,created.StatusCode);
        using var duplicate=new HttpRequestMessage(HttpMethod.Post,$"/api/public/orders/{owned.Order.Id.Value}/feedback"){Content=JsonContent.Create(new{OrderLineId=line,Rating=4,Comment="again"})};duplicate.Headers.Add("X-Dining-Session",owned.Token);
        Assert.Equal(HttpStatusCode.Conflict,(await client.SendAsync(duplicate)).StatusCode);
    }
    [Fact] public async Task ManagementReadsArePermissionAndTenantScopedAndModerationChangesSummary()
    {
        await factory.MigrateDatabaseAsync();await factory.MigrateOrderingDatabaseAsync();await factory.MigrateFeedbackDatabaseAsync();
        var restaurant=await factory.SeedRestaurantAsync($"Feedback {Guid.NewGuid():N}",DateTimeOffset.UtcNow);var rid=restaurant.Id.Value;var bid=Guid.NewGuid();
        var guest=await factory.SeedGuestOrderAsync(rid,bid,OrderStatus.Completed);using var publicClient=factory.CreateClient();
        using(var create=new HttpRequestMessage(HttpMethod.Post,$"/api/public/orders/{guest.Order.Id.Value}/feedback"){Content=JsonContent.Create(new{Rating=2,Comment="slow"})}){create.Headers.Add("X-Dining-Session",guest.Token);(await publicClient.SendAsync(create)).EnsureSuccessStatusCode();}
        using var client=factory.CreateClient();using var list=await client.GetAsync($"/api/restaurants/{rid}/feedback?pageNumber=1&pageSize=20");list.EnsureSuccessStatusCode();var items=await list.Content.ReadFromJsonAsync<JsonElement>();Assert.Equal(1,items.GetArrayLength());var id=items[0].GetProperty("id").GetGuid();
        using var hide=await client.PostAsJsonAsync($"/api/restaurants/{rid}/feedback/{id}/hide",new{ExpectedVersion=1});hide.EnsureSuccessStatusCode();
        using var summary=await client.GetAsync($"/api/restaurants/{rid}/feedback/summary");summary.EnsureSuccessStatusCode();var body=await summary.Content.ReadFromJsonAsync<JsonElement>();Assert.Equal(0,body.GetProperty("totalCount").GetInt32());
        using var deniedRequest=new HttpRequestMessage(HttpMethod.Get,$"/api/restaurants/{rid}/feedback");deniedRequest.Headers.Add(TestAuthenticationHandler.PermissionsHeader,"none");
        Assert.Equal(HttpStatusCode.Forbidden,(await client.SendAsync(deniedRequest)).StatusCode);
        using var otherTenant=await client.GetAsync($"/api/restaurants/{Guid.NewGuid()}/feedback");Assert.Equal(HttpStatusCode.Forbidden,otherTenant.StatusCode);
    }
}
