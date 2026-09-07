using System.Diagnostics;

using Microsoft.Extensions.Primitives;

namespace RestaurantMenu.Api.Observability;

public sealed partial class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemName = "CorrelationId";

    private const int MaxCorrelationIdLength = 64;

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(logger);
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = GetOrCreateCorrelationId(context);
        context.Items[ItemName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        Activity.Current?.SetTag("correlation.id", correlationId);

        using var scope = _logger.BeginScope(
            new Dictionary<string, object?>
            {
                [ItemName] = correlationId
            });
        var isHealthProbe =
            context.Request.Path.StartsWithSegments("/health");
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            await _next(context);
        }
        finally
        {
            if (!isHealthProbe &&
                _logger.IsEnabled(LogLevel.Information))
            {
                var elapsedMilliseconds =
                    Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

                LogRequestCompleted(
                    _logger,
                    context.Request.Method,
                    context.Request.Path.Value ?? string.Empty,
                    context.Response.StatusCode,
                    elapsedMilliseconds);
            }
        }
    }

    public static string? GetCorrelationId(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Items.TryGetValue(ItemName, out var value)
            ? value as string
            : null;
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(
                HeaderName,
                out var values) &&
            IsValid(values))
        {
            return values[0]!;
        }

        return Activity.Current?.TraceId.ToString() ??
               Guid.CreateVersion7().ToString("N");
    }

    private static bool IsValid(StringValues values)
    {
        if (values.Count != 1 ||
            string.IsNullOrWhiteSpace(values[0]) ||
            values[0]!.Length > MaxCorrelationIdLength)
        {
            return false;
        }

        return values[0]!.All(
            character =>
                char.IsAsciiLetterOrDigit(character) ||
                character is '-' or '_' or '.');
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds:F3} ms.")]
    private static partial void LogRequestCompleted(
        ILogger logger,
        string method,
        string path,
        int statusCode,
        double elapsedMilliseconds);
}
