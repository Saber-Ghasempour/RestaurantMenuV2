using System.Net;
using System.Net.Http.Json;

namespace RestaurantMenu.Api.FunctionalTests.Infrastructure;

public sealed class HealthCheckTests
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public HealthCheckTests(TestWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    [Fact]
    public async Task LivenessShouldReportHealthyProcess()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var health =
            await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.NotNull(health);
        Assert.Equal("Healthy", health.Status);
        Assert.Collection(
            health.Checks,
            check =>
            {
                Assert.Equal("self", check.Key);
                Assert.Equal("Healthy", check.Value.Status);
            });
    }

    [Fact]
    public async Task ReadinessShouldReportAllDatabasesHealthy()
    {
        await _factory.MigrateDatabaseAsync();
        await _factory.MigrateCatalogDatabaseAsync();
        await _factory.MigrateMediaDatabaseAsync();
        await _factory.MigrateOrderingDatabaseAsync();

        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var health =
            await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.NotNull(health);
        Assert.Equal("Healthy", health.Status);
        Assert.Equal(5, health.Checks.Count);
        Assert.Equal(
            "Healthy",
            health.Checks["restaurants-database"].Status);
        Assert.Equal(
            "Healthy",
            health.Checks["catalog-database"].Status);
        Assert.Equal(
            "Healthy",
            health.Checks["media-database"].Status);
        Assert.Equal(
            "Healthy",
            health.Checks["ordering-database"].Status);
        Assert.Equal(
            "Healthy",
            health.Checks["redis"].Status);
    }

    private sealed record HealthResponse(
        string Status,
        double TotalDurationMilliseconds,
        IReadOnlyDictionary<string, HealthEntryResponse> Checks);

    private sealed record HealthEntryResponse(
        string Status,
        double DurationMilliseconds);
}
