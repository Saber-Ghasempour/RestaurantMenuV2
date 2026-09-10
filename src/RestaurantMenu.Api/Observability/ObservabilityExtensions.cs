using Npgsql;

using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RestaurantMenu.Ordering.Infrastructure.Messaging;

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
                    .AddSource(MessagingTelemetry.ActivitySourceName)
                    .AddAspNetCoreInstrumentation(
                        options =>
                            options.Filter = context =>
                                !context.Request.Path.StartsWithSegments(
                                    "/health") &&
                                !context.Request.Path.StartsWithSegments(
                                    "/m") &&
                                !context.Request.Path.StartsWithSegments(
                                    "/api/public/menu-codes"))
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
                    .AddRuntimeInstrumentation()
                    .AddMeter(MessagingTelemetry.MeterName)
                    .AddMeter(ApiTelemetry.MeterName);

                if (hasOtlpExporter)
                {
                    metrics.AddOtlpExporter();
                }
            });

        return services;
    }

    public static ILoggingBuilder AddRestaurantMenuTelemetryLogging(
        this ILoggingBuilder logging,
        IConfiguration configuration)
    {
        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? DefaultServiceName;
        var endpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ??
            configuration["OpenTelemetry:OtlpEndpoint"];
        logging.Configure(options => options.ActivityTrackingOptions =
            ActivityTrackingOptions.TraceId |
            ActivityTrackingOptions.SpanId |
            ActivityTrackingOptions.ParentId);
        logging.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            if (Uri.TryCreate(endpoint, UriKind.Absolute, out _))
                options.AddOtlpExporter();
        });
        return logging;
    }
}
