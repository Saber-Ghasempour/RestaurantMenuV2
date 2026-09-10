using Microsoft.Extensions.Diagnostics.HealthChecks;
using RestaurantMenu.Ordering.Infrastructure.Messaging;

namespace RestaurantMenu.Api.Health;

public sealed class RabbitMqHealthCheck(IRabbitMqConnection connection) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return await connection.ProbeAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("RabbitMQ is unavailable.");
    }
}
