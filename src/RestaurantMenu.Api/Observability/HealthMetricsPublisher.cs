using System.Diagnostics.Metrics;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RestaurantMenu.Api.Observability;

public sealed class HealthMetricsPublisher : IHealthCheckPublisher
{
    private static readonly string[] KnownDependencies =
    [
        "restaurants-database",
        "catalog-database",
        "media-database",
        "ordering-database",
        "feedback-database",
        "redis",
        "rabbitmq"
    ];
    private static readonly Meter Meter = new(ApiTelemetry.MeterName);
    private static readonly Lock Sync = new();
    private static Dictionary<string, long> _statuses = KnownDependencies
        .ToDictionary(value => value, _ => 0L, StringComparer.Ordinal);

    static HealthMetricsPublisher()
    {
        Meter.CreateObservableGauge("restaurantmenu.dependencies.health", Observe);
    }

    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);
        lock (Sync)
        {
            _statuses = KnownDependencies.ToDictionary(
                name => name,
                name => report.Entries.TryGetValue(name, out var entry) &&
                    entry.Status == HealthStatus.Healthy ? 1L : 0L,
                StringComparer.Ordinal);
        }
        return Task.CompletedTask;
    }

    private static IEnumerable<Measurement<long>> Observe()
    {
        KeyValuePair<string, long>[] snapshot;
        lock (Sync)
            snapshot = _statuses.ToArray();
        return snapshot.Select(value => new Measurement<long>(value.Value,
            new KeyValuePair<string, object?>("dependency", value.Key)));
    }
}
