using System.Net;
using System.Net.Http.Json;

using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace RestaurantMenu.Api.FunctionalTests.Infrastructure;

public sealed class ObservabilityTests
    : IClassFixture<TestWebApplicationFactory>
{
    private const string CorrelationHeader = "X-Correlation-ID";

    private readonly TestWebApplicationFactory _factory;

    public ObservabilityTests(TestWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    [Fact]
    public async Task ValidCorrelationIdShouldBeEchoedAndAddedToProblemDetails()
    {
        const string correlationId = "interview-demo-123";
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/restaurants")
        {
            Content = JsonContent.Create(
                new CreateRestaurantRequest("   "))
        };
        request.Headers.Add(CorrelationHeader, correlationId);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            correlationId,
            Assert.Single(
                response.Headers.GetValues(CorrelationHeader)));
        var problem = await response.Content
            .ReadFromJsonAsync<ProblemResponse>();
        Assert.NotNull(problem);
        Assert.Equal(correlationId, problem.CorrelationId);
        Assert.False(string.IsNullOrWhiteSpace(problem.TraceId));
    }

    [Fact]
    public async Task InvalidCorrelationIdShouldBeReplaced()
    {
        var invalidCorrelationId = new string('a', 65);
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/restaurants")
        {
            Content = JsonContent.Create(
                new CreateRestaurantRequest("   "))
        };
        request.Headers.Add(
            CorrelationHeader,
            invalidCorrelationId);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var returnedCorrelationId = Assert.Single(
            response.Headers.GetValues(CorrelationHeader));
        Assert.NotEqual(
            invalidCorrelationId,
            returnedCorrelationId);
        Assert.False(
            string.IsNullOrWhiteSpace(returnedCorrelationId));
        Assert.True(returnedCorrelationId.Length <= 64);
    }

    [Fact]
    public void OpenTelemetryProvidersShouldBeRegistered()
    {
        Assert.NotNull(
            _factory.Services.GetService<TracerProvider>());
        Assert.NotNull(
            _factory.Services.GetService<MeterProvider>());
    }

    private sealed record CreateRestaurantRequest(string? Name);

    private sealed record ProblemResponse(
        string TraceId,
        string CorrelationId);
}
