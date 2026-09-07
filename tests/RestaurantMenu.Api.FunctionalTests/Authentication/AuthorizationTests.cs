using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using RestaurantMenu.Api.FunctionalTests.Infrastructure;
using RestaurantMenu.Presentation.Abstractions.Authorization;

namespace RestaurantMenu.Api.FunctionalTests.Authentication;

public sealed class AuthorizationTests
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AuthorizationTests(
        TestWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    [Fact]
    public async Task AnonymousRequestToProtectedEndpointShouldReturnUnauthorized()
    {
        using var client = CreateClient(
            TestAuthenticationHandler.AnonymousIdentity);

        using var response = await client.GetAsync(
            $"/api/restaurants/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
        Assert.Contains(
            TestAuthenticationHandler.AuthenticationScheme,
            response.Headers.WwwAuthenticate.Select(header => header.Scheme));

        var problemDetails =
            await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problemDetails);
        Assert.Equal(
            StatusCodes.Status401Unauthorized,
            problemDetails.Status);
        Assert.Equal(
            "Unauthorized",
            problemDetails.Title);
        Assert.True(problemDetails.Extensions.ContainsKey("traceId"));
        Assert.True(problemDetails.Extensions.ContainsKey("correlationId"));
    }

    [Fact]
    public async Task AuthenticatedRequestWithoutPermissionShouldReturnForbidden()
    {
        using var client = CreateClient(
            "authenticated",
            "none");

        using var response = await client.GetAsync(
            $"/api/restaurants/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        var problemDetails =
            await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problemDetails);
        Assert.Equal(
            StatusCodes.Status403Forbidden,
            problemDetails.Status);
        Assert.Equal(
            "Forbidden",
            problemDetails.Title);
    }

    [Fact]
    public async Task PermissionFromAnotherModuleShouldReturnForbidden()
    {
        using var client = CreateClient(
            "authenticated",
            Permissions.CatalogRead);

        using var response = await client.GetAsync(
            $"/api/restaurants/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task PermissionWithoutMembershipShouldReturnForbidden()
    {
        await _factory.MigrateDatabaseAsync();

        using var client = CreateClient(
            "authenticated",
            Permissions.RestaurantsRead);

        using var response = await client.GetAsync(
            $"/api/restaurants/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task PermissionAndMembershipShouldAuthorizeRestaurantRead()
    {
        await _factory.MigrateDatabaseAsync();

        var restaurant = await _factory.SeedRestaurantAsync(
            "Authorized Restaurant",
            new DateTimeOffset(
                2026,
                9,
                7,
                20,
                0,
                0,
                TimeSpan.Zero));

        using var client = CreateClient(
            "authenticated",
            Permissions.RestaurantsRead);

        using var response = await client.GetAsync(
            $"/api/restaurants/{restaurant.Id.Value}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MembershipForAnotherSubjectShouldReturnForbidden()
    {
        await _factory.MigrateDatabaseAsync();

        var restaurant = await _factory.SeedRestaurantAsync(
            "Tenant A Restaurant",
            new DateTimeOffset(
                2026,
                9,
                7,
                20,
                30,
                0,
                TimeSpan.Zero),
            "tenant-a-user");

        using var client = CreateClient(
            "authenticated",
            Permissions.RestaurantsRead,
            "tenant-b-user");

        using var response = await client.GetAsync(
            $"/api/restaurants/{restaurant.Id.Value}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReadPermissionShouldNotAuthorizeRestaurantWrite()
    {
        using var client = CreateClient(
            "authenticated",
            Permissions.RestaurantsRead);

        using var response = await client.PostAsJsonAsync(
            "/api/restaurants/",
            new { Name = "Unauthorized write" });

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task HealthEndpointShouldAllowAnonymousRequests()
    {
        using var client = CreateClient(
            TestAuthenticationHandler.AnonymousIdentity);

        using var response = await client.GetAsync(
            "/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private HttpClient CreateClient(
        string identity,
        string? permissions = null,
        string? subject = null)
    {
        var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            TestAuthenticationHandler.IdentityHeader,
            identity);

        if (permissions is not null)
        {
            client.DefaultRequestHeaders.Add(
                TestAuthenticationHandler.PermissionsHeader,
                permissions);
        }

        if (subject is not null)
        {
            client.DefaultRequestHeaders.Add(
                TestAuthenticationHandler.SubjectHeader,
                subject);
        }

        return client;
    }
}
