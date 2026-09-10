using System.Net;using System.Text;using RestaurantMenu.Restaurants.Infrastructure.Memberships;
namespace RestaurantMenu.Restaurants.IntegrationTests.Memberships;
public sealed class KeycloakIdentityProvisionerTests
{
 [Fact]public async Task ExistingIdentityIsResolvedUsingClientCredentialsWithoutExposingSecret()
 {var handler=new StubHandler(new([Json(HttpStatusCode.OK,"{\"access_token\":\"admin-token\"}"),Json(HttpStatusCode.OK,"[{\"id\":\"subject-1\"}]")]));var client=new HttpClient(handler);var sut=new KeycloakIdentityProvisioner(client,new(new("https://id.example/"),"restaurant-menu","admin-client","secret"));Assert.Equal("subject-1",await sut.EnsureUserAsync("user@example.com",default));Assert.Equal(2,handler.Requests.Count);Assert.Equal("Bearer",handler.Requests[1].Headers.Authorization?.Scheme);Assert.DoesNotContain("secret",handler.Requests[1].RequestUri!.ToString());}
 private static HttpResponseMessage Json(HttpStatusCode status,string json)=>new(status){Content=new StringContent(json,Encoding.UTF8,"application/json")};
 private sealed class StubHandler(Queue<HttpResponseMessage> responses):HttpMessageHandler
 {public List<HttpRequestMessage> Requests{get;}=[];protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken){Requests.Add(request);return Task.FromResult(responses.Dequeue());}}
}
