using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using RestaurantMenu.Restaurants.Application.Abstractions.Security;
namespace RestaurantMenu.Restaurants.Infrastructure.Memberships;

public sealed record KeycloakAdminOptions(Uri BaseUri, string Realm, string ClientId, string ClientSecret);
public sealed class KeycloakIdentityProvisioner(HttpClient client, KeycloakAdminOptions options) : IIdentityProvisioner
{
    public async Task<string?> EnsureUserAsync(string email, CancellationToken cancellationToken) { var token = await GetToken(cancellationToken); if (token is null) return null; var existing = await Find(email, token, cancellationToken); if (existing is not null) return existing; using var create = new HttpRequestMessage(HttpMethod.Post, new Uri(options.BaseUri, $"admin/realms/{Uri.EscapeDataString(options.Realm)}/users")) { Content = JsonContent.Create(new { username = email, email, enabled = true, emailVerified = false }) }; create.Headers.Authorization = new("Bearer", token); using var response = await client.SendAsync(create, cancellationToken); if (response.StatusCode is not HttpStatusCode.Created and not HttpStatusCode.Conflict) return null; return await Find(email, token, cancellationToken); }
    private async Task<string?> GetToken(CancellationToken ct) { using var content = new FormUrlEncodedContent(new Dictionary<string, string> { { "grant_type", "client_credentials" }, { "client_id", options.ClientId }, { "client_secret", options.ClientSecret } }); using var response = await client.PostAsync(new Uri(options.BaseUri, $"realms/{Uri.EscapeDataString(options.Realm)}/protocol/openid-connect/token"), content, ct); if (!response.IsSuccessStatusCode) return null; var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct); return json.TryGetProperty("access_token", out var value) ? value.GetString() : null; }
    private async Task<string?> Find(string email, string token, CancellationToken ct) { using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(options.BaseUri, $"admin/realms/{Uri.EscapeDataString(options.Realm)}/users?email={Uri.EscapeDataString(email)}&exact=true")); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token); using var response = await client.SendAsync(request, ct); if (!response.IsSuccessStatusCode) return null; var users = await response.Content.ReadFromJsonAsync<JsonElement>(ct); return users.ValueKind == JsonValueKind.Array && users.GetArrayLength() > 0 && users[0].TryGetProperty("id", out var id) ? id.GetString() : null; }
}