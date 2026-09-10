using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RestaurantMenu.Api.Infrastructure;

namespace RestaurantMenu.Api.FunctionalTests.Infrastructure;

public sealed class ApiPolicyUnitTests
{
    [Fact]
    public async Task IdempotencyMiddlewareMarksMissingKeyForCapabilityFirstEndpointValidation()
    {
        var endpointCalled = false;
        var middleware = new IdempotencyMiddleware(_ =>
        {
            endpointCalled = true;
            return Task.CompletedTask;
        });
        var context = NewContext("POST", "/api/public/orders");

        await middleware.InvokeAsync(context);

        Assert.True(endpointCalled);
        Assert.True(context.Items.ContainsKey(IdempotencyMiddleware.InvalidItemName));
    }

    [Fact]
    public async Task IdempotencyMiddlewarePassesValidatedKey()
    {
        const string key = "request-123";
        object? observed = null;
        var middleware = new IdempotencyMiddleware(context =>
        {
            observed = context.Items[IdempotencyMiddleware.ItemName];
            return Task.CompletedTask;
        });
        var context = NewContext("POST", "/api/public/orders");
        context.Request.Headers[IdempotencyMiddleware.HeaderName] = key;

        await middleware.InvokeAsync(context);

        Assert.Equal(key, observed);
    }

    [Fact]
    public async Task ConcurrencyMiddlewareRejectsWeakEntityTag()
    {
        var middleware = new ApiConcurrencyMiddleware(_ => Task.CompletedTask);
        var context = NewContext("PUT", "/api/restaurants/00000000-0000-0000-0000-000000000001");
        context.Request.Headers.IfMatch = "W/\"1\"";

        await middleware.InvokeAsync(context);

        Assert.Equal(HttpStatusCode.BadRequest, (HttpStatusCode)context.Response.StatusCode);
    }

    [Fact]
    public async Task ConcurrencyMiddlewareEmitsStrongEtagFromVersionedRepresentation()
    {
        var middleware = new ApiConcurrencyMiddleware(async context =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"id\":\"value\",\"version\":7}");
        });
        var context = NewContext("GET", "/api/resources/value");

        await middleware.InvokeAsync(context);

        Assert.Equal("\"7\"", context.Response.Headers.ETag);
    }

    [Fact]
    public void DiningSessionRateLimitPartitionIsStableAndSecretSafe()
    {
        var first = RateLimitPartitionKeys.ForDiningSession("secret-token");
        var second = RateLimitPartitionKeys.ForDiningSession("secret-token");

        Assert.Equal(first, second);
        Assert.DoesNotContain("secret-token", first, StringComparison.Ordinal);
        Assert.NotEqual(first, RateLimitPartitionKeys.ForDiningSession("another-token"));
    }

    [Fact]
    public async Task RequestTimeoutCancelsEndpointAndReturnsGatewayTimeout()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Services.AddRequestTimeouts(options => options.DefaultPolicy = new RequestTimeoutPolicy
        {
            Timeout = TimeSpan.FromMilliseconds(20),
            TimeoutStatusCode = StatusCodes.Status504GatewayTimeout
        });
        await using var app = builder.Build();
        app.UseRequestTimeouts();
        app.MapGet("/slow", async (CancellationToken cancellationToken) =>
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken));
        await app.StartAsync();

        using var response = await app.GetTestClient().GetAsync("/slow");

        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
    }

    private static DefaultHttpContext NewContext(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.RequestServices = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        return context;
    }
}
