using System.Diagnostics;
using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging;

using RestaurantMenu.Api.Observability;

namespace RestaurantMenu.Api.FunctionalTests.Infrastructure;

public sealed class AuditLoggingTests
{
    [Fact]
    public async Task AuthenticatedMutationShouldLogBoundedRedactedAuditFields()
    {
        const string rawSubject = "person@example.com";
        const string diningToken = "super-secret-dining-token";
        var logger = new RecordingLogger<RequestAuditMiddleware>();
        var middleware = new RequestAuditMiddleware(
            context =>
            {
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            },
            logger);
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Put;
        context.Request.Path = "/api/restaurants/11111111-1111-1111-1111-111111111111";
        context.Request.QueryString = new QueryString($"?token={diningToken}");
        context.Items[CorrelationIdMiddleware.ItemName] = "correlation-123";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", rawSubject)],
            authenticationType: "test"));
        context.SetEndpoint(new RouteEndpoint(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse("/api/restaurants/{restaurantId:guid}"),
            order: 0,
            EndpointMetadataCollection.Empty,
            displayName: null));

        using var activity = new Activity("test-request").Start();
        await middleware.InvokeAsync(context);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("ManagementMutation", entry.Properties["AuditType"]);
        Assert.Equal("PUT", entry.Properties["Method"]);
        Assert.Equal("/api/restaurants/{restaurantId:guid}", entry.Properties["Route"]);
        Assert.Equal("Succeeded", entry.Properties["Outcome"]);
        Assert.Equal("correlation-123", entry.Properties["CorrelationId"]);
        Assert.Equal(activity.TraceId.ToString(), entry.Properties["TraceId"]);
        Assert.Matches("^[a-f0-9]{16}$", Assert.IsType<string>(entry.Properties["ActorHash"]));
        Assert.DoesNotContain(rawSubject, entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(diningToken, entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(rawSubject, string.Join('|', entry.Properties.Values),
            StringComparison.Ordinal);
        Assert.DoesNotContain(diningToken, string.Join('|', entry.Properties.Values),
            StringComparison.Ordinal);
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<Entry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var properties = state is IEnumerable<KeyValuePair<string, object?>> values
                ? values.ToDictionary(value => value.Key, value => value.Value)
                : [];
            Entries.Add(new Entry(logLevel, formatter(state, exception), properties));
        }
    }

    private sealed record Entry(LogLevel Level, string Message,
        IReadOnlyDictionary<string, object?> Properties);
}
