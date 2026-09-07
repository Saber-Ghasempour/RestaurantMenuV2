using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RestaurantMenu.Api.Health;

public static class HealthCheckResponseWriter
{
    public static Task WriteAsync(
        HttpContext httpContext,
        HealthReport report)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(report);

        var response = new HealthResponse(
            report.Status.ToString(),
            report.TotalDuration.TotalMilliseconds,
            report.Entries
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .ToDictionary(
                    entry => entry.Key,
                    entry => new HealthEntryResponse(
                        entry.Value.Status.ToString(),
                        entry.Value.Duration.TotalMilliseconds),
                    StringComparer.Ordinal));

        return httpContext.Response.WriteAsJsonAsync(response);
    }

    private sealed record HealthResponse(
        string Status,
        double TotalDurationMilliseconds,
        IReadOnlyDictionary<string, HealthEntryResponse> Checks);

    private sealed record HealthEntryResponse(
        string Status,
        double DurationMilliseconds);
}
