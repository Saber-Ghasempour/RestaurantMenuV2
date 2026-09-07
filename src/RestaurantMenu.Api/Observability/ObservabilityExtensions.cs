using Npgsql;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace RestaurantMenu.Api.Observability;

public static class ObservabilityExtensions
{
    private const string DefaultServiceName = "RestaurantMenu.Api";

    public static IServiceCollection AddRestaurantMenuObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var serviceName =
            configuration["OpenTelemetry:ServiceName"] ??
            DefaultServiceName;
        var otlpEndpoint =
            configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ??
            configuration["OpenTelemetry:OtlpEndpoint"];
        var hasOtlpExporter = Uri.TryCreate(
            otlpEndpoint,
            UriKind.Absolute,
            out _);

        var openTelemetry = services
            .AddOpenTelemetry()
            .ConfigureResource(
                resource => resource.AddService(serviceName));

        openTelemetry.WithTracing(
            tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddNpgsql();

                if (hasOtlpExporter)
                {
                    tracing.AddOtlpExporter();
                }
            });

        openTelemetry.WithMetrics(
            metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (hasOtlpExporter)
                {
                    metrics.AddOtlpExporter();
                }
            });

        return services;
    }
}
