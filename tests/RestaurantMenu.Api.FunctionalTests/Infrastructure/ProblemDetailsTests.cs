using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Restaurants.GetRestaurant;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Api.FunctionalTests.Infrastructure;

public sealed class ProblemDetailsTests
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ProblemDetailsTests(TestWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    [Fact]
    public async Task UnhandledExceptionShouldReturnSafeProblemDetails()
    {
        using var application = _factory.WithWebHostBuilder(
            builder =>
                builder.ConfigureTestServices(
                    services =>
                    {
                        services.RemoveAll<
                            IQueryHandler<
                                GetRestaurantQuery,
                                Result<RestaurantResponse>>>();
                        services.AddScoped<
                            IQueryHandler<
                                GetRestaurantQuery,
                                Result<RestaurantResponse>>,
                            ThrowingGetRestaurantQueryHandler>();
                    }));
        using var client = application.CreateClient();

        using var response = await client.GetAsync(
            $"/api/restaurants/{Guid.CreateVersion7()}");

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content
            .ReadFromJsonAsync<ProblemResponse>();
        Assert.NotNull(problem);
        Assert.Equal(500, problem.Status);
        Assert.Equal("Server.UnexpectedError", problem.Title);
        Assert.Equal(
            "An unexpected error occurred.",
            problem.Detail);
        Assert.False(string.IsNullOrWhiteSpace(problem.TraceId));
        Assert.DoesNotContain(
            ThrowingGetRestaurantQueryHandler.SensitiveMessage,
            await response.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExpectedValidationFailureShouldIncludeTraceIdentifier()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/restaurants",
            new CreateRestaurantRequest("   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content
            .ReadFromJsonAsync<ValidationProblemResponse>();
        Assert.NotNull(problem);
        Assert.False(string.IsNullOrWhiteSpace(problem.TraceId));
        Assert.True(
            problem.Errors.ContainsKey(
                "Restaurants.NameRequired"));
    }

    private sealed class ThrowingGetRestaurantQueryHandler
        : IQueryHandler<
            GetRestaurantQuery,
            Result<RestaurantResponse>>
    {
        public const string SensitiveMessage =
            "Sensitive database failure details.";

        public Task<Result<RestaurantResponse>> Handle(
            GetRestaurantQuery query,
            CancellationToken cancellationToken) =>
            Task.FromException<Result<RestaurantResponse>>(
                new InvalidOperationException(SensitiveMessage));
    }

    private sealed record CreateRestaurantRequest(string? Name);

    private sealed record ProblemResponse(
        int Status,
        string Title,
        string Detail,
        string TraceId);

    private sealed record ValidationProblemResponse(
        Dictionary<string, string[]> Errors,
        string TraceId);
}
